using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

// CMU14: persistent delivery outbox. A lease makes retries safe across multiple game servers.
public partial interface IServerDbManager
{
    Task<KabanDiscordRole?> KabanClaimDiscord(string lease, long now);
    Task<bool> KabanRecordDiscord(Guid user, Guid delivery, string lease, string role, bool complete, long now);
}

public sealed partial class ServerDbManager
{
    public Task<KabanDiscordRole?> KabanClaimDiscord(string lease, long now)
        => RunDbCommand(() => _db.KabanClaimDiscord(lease, now));
    public Task<bool> KabanRecordDiscord(Guid user, Guid delivery, string lease, string role, bool complete, long now)
        => RunDbCommand(() => _db.KabanRecordDiscord(user, delivery, lease, role, complete, now));
}

public abstract partial class ServerDbBase
{
    public async Task<KabanDiscordRole?> KabanClaimDiscord(string lease, long now)
    {
        await using var guard = await GetDb();
        var db = guard.DbContext;
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1");
        var role = await db.Set<KabanDiscordRole>()
            .Where(r => (r.Status == "approved" || r.Status == "delivering") && r.LeaseUntil < now)
            .OrderBy(r => r.LeaseUntil).FirstOrDefaultAsync();
        if (role == null)
            return null;
        // Re-linking never redirects an already purchased role to an unreviewed account.
        var linked = await db.RMCLinkedAccounts.Where(a => a.PlayerId == role.UserId).Select(a => a.DiscordId).SingleOrDefaultAsync();
        if (linked.ToString() != role.DiscordId)
        {
            role.Status = "pending";
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return null;
        }
        role.Lease = lease;
        role.LeaseUntil = now + TimeSpan.FromMinutes(2).Ticks;
        role.Status = "delivering";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return role;
    }

    public async Task<bool> KabanRecordDiscord(Guid user, Guid delivery, string lease, string roleId, bool complete, long now)
    {
        await using var guard = await GetDb();
        var db = guard.DbContext;
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1");
        var role = await db.Set<KabanDiscordRole>().SingleOrDefaultAsync(r => r.UserId == user);
        if (role == null || role.Delivery != delivery || role.Lease != lease || role.Status != "delivering" || role.LeaseUntil < now)
            return false;
        role.RoleId = roleId;
        if (complete)
        {
            role.Status = "active";
            role.LeaseUntil = 0;
            db.Add(new KabanAudit
            {
                Id = Guid.NewGuid(), Operation = delivery, Actor = Guid.Empty, Target = user,
                Action = "DiscordDelivered", Detail = roleId, At = now,
            });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }
}
