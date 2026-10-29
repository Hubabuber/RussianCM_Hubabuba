using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Content.Server.Database;

public enum DiscordLinkStatus { Success, AlreadyLinked, InvalidCode, Expired, Conflict }
public sealed record DiscordLinkResult(DiscordLinkStatus Status, Guid Player = default);

public partial interface IServerDbManager
{
    Task<DiscordLinkResult> CompleteDiscordLink(Guid code, ulong discord, DateTime now);
}

public sealed partial class ServerDbManager
{
    public Task<DiscordLinkResult> CompleteDiscordLink(Guid code, ulong discord, DateTime now)
        => RunDbCommand(() => _db.CompleteDiscordLink(code, discord, now));
}

public abstract partial class ServerDbBase
{
    public async Task<DiscordLinkResult> CompleteDiscordLink(Guid code, ulong discord, DateTime now)
    {
        await using var guard = await GetDb();
        return await DiscordAccountLinking.Complete(guard.DbContext, code, discord, now);
    }
}

/// <summary>Uses the game's configured SQLite/PostgreSQL database and existing account tables.</summary>
public static class DiscordAccountLinking
{
    public static async Task<DiscordLinkResult> Complete(ServerDbContext db, Guid code, ulong discord, DateTime now)
    {
        if (code == Guid.Empty || discord == 0)
            return new(DiscordLinkStatus.InvalidCode);
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Acquire the code row before reading. SQLite also acquires its writer lock here.
        var matches = await db.RMCLinkingCodes.Where(c => c.Code == code)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Code, code));
        if (matches != 1)
            return new(DiscordLinkStatus.InvalidCode);
        var linking = await db.RMCLinkingCodes.SingleAsync(c => c.Code == code);
        if (linking.CreationTime <= now.AddDays(-1) || linking.CreationTime > now)
            return new(DiscordLinkStatus.Expired);
        var existing = await db.RMCLinkedAccounts
            .Where(l => l.PlayerId == linking.PlayerId || l.DiscordId == discord).ToArrayAsync();
        // Never silently detach somebody else's account or remove their patron privileges.
        if (existing.Any(l => l.PlayerId != linking.PlayerId || l.DiscordId != discord))
            return new(DiscordLinkStatus.Conflict);
        var alreadyLinked = existing.Length != 0;
        if (!alreadyLinked)
        {
            if (!await db.RMCDiscordAccounts.AnyAsync(a => a.Id == discord))
                db.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = discord });
            db.RMCLinkedAccounts.Add(new RMCLinkedAccount { PlayerId = linking.PlayerId, DiscordId = discord });
            db.RMCLinkedAccountLogs.Add(new RMCLinkedAccountLogs
                { PlayerId = linking.PlayerId, DiscordId = discord, At = now });
        }
        db.RMCLinkingCodes.Remove(linking);
        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" }
            or SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        {
            // Two distinct codes racing for one Discord account: the unique index decides once.
            await transaction.RollbackAsync();
            return new(DiscordLinkStatus.Conflict);
        }
        return new(alreadyLinked ? DiscordLinkStatus.AlreadyLinked : DiscordLinkStatus.Success, linking.PlayerId);
    }
}
