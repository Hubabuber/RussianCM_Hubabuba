using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RuCM.Kaban;
using Microsoft.EntityFrameworkCore;

namespace Content.Server._RuCM.Kaban;

// Metadata lives in the already migrated round-request envelope. No production schema change is needed.
public sealed record KabanForcePayload(string Server, KabanForceChoice Choice, string Attempt = "");

public static partial class KabanEconomy
{
    public static KabanForcePayload? ForcePayload(KabanRoundRequest request)
    {
        try { return JsonSerializer.Deserialize<KabanForcePayload>(request.Parameters); }
        catch (JsonException) { return null; }
    }

    private static async Task<bool> ForceCooldown(ServerDbContext db, Guid user, string product, long now)
        => await db.Set<KabanRoundRequest>().AnyAsync(q => q.UserId == user && q.Status == "completed"
            && (q.CompletedAt > now - TimeSpan.TicksPerDay * 7
                || product == "full" && q.ProductId == "full" && q.CompletedAt > now - TimeSpan.TicksPerDay * 30));

    private static async Task PurchaseForcing(ServerDbContext db, KabanPolicy policy, KabanRequest request, int price, long now)
    {
        var choice = KabanForceChoice.Decode(request.Text);
        Require(choice != null && choice.ValidFor(request.Product)
            && policy.ForceOptions != null && policy.ForceOptions.Any(choice.Matches), "cmu-kaban-force-incompatible");
        Require(!await db.Set<KabanRoundRequest>().AnyAsync(q => q.UserId == policy.Actor
            && (q.Status == "queued" || q.Status == "applying")), "cmu-kaban-force-pending");
        Require(!await ForceCooldown(db, policy.Actor, request.Product, now), "cmu-kaban-force-cooldown");
        Require(await db.Set<KabanRoundRequest>().CountAsync(q => q.Status == "queued" || q.Status == "applying") < 1000,
            "cmu-kaban-inventory-limit");
        db.Add(new KabanRoundRequest
        {
            Id = request.Operation, UserId = policy.Actor, ProductId = request.Product, Price = price, At = now,
            Parameters = JsonSerializer.Serialize(new KabanForcePayload(policy.Server, choice!)),
        });
    }

    /// <summary>The economic lock elects one FIFO head per server, even with concurrent claims/cancellations.</summary>
    public static async Task<KabanRoundRequest?> ClaimForce(ServerDbContext db, string server, string attempt, long now)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        Require(await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1") == 1,
            "cmu-kaban-storage-unavailable");
        Require(!string.IsNullOrWhiteSpace(attempt) && !string.IsNullOrWhiteSpace(server));
        var processed = await db.Set<KabanRoundRequest>().Where(q => q.Status != "queued" && q.Parameters.Contains(attempt)).ToListAsync();
        if (processed.Any(q => ForcePayload(q) is { } p && p.Server == server && p.Attempt == attempt))
            return null;
        var pending = await db.Set<KabanRoundRequest>().Where(q => q.Status == "queued" || q.Status == "applying")
            .OrderBy(q => q.At).ThenBy(q => q.Id).ToListAsync();
        if (pending.Any(q => q.Status == "applying" && ForcePayload(q)?.Server == server))
            return null;
        var head = pending.FirstOrDefault(q => q.Status == "queued" && ForcePayload(q)?.Server == server);
        if (head == null)
            return null;
        head.Parameters = JsonSerializer.Serialize(ForcePayload(head)! with { Attempt = attempt });
        if (await ForceCooldown(db, head.UserId, head.ProductId, now))
            await RefundForce(db, head, now, "cooldown");
        else
        {
            head.Status = "applying";
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return head.Status == "applying" ? head : null;
    }

    public static async Task FinishForce(ServerDbContext db, string server, Guid id, string attempt, bool used, long now, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        Require(await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1") == 1,
            "cmu-kaban-storage-unavailable");
        var request = await db.Set<KabanRoundRequest>().SingleOrDefaultAsync(q => q.Id == id);
        if (request == null || request.Status != "applying" || ForcePayload(request) is not { } payload
            || payload.Server != server || payload.Attempt != attempt)
            return;
        if (used)
        {
            request.Status = "completed";
            request.CompletedAt = now;
            db.Add(new KabanAudit { Id = Guid.NewGuid(), Operation = id, Target = request.UserId,
                Action = "ForceCompleted", Detail = reason, At = now });
        }
        else
            await RefundForce(db, request, now, reason);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public static async Task RecoverForce(ServerDbContext db, string server, long now)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        Require(await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1") == 1,
            "cmu-kaban-storage-unavailable");
        foreach (var request in await db.Set<KabanRoundRequest>().Where(q => q.Status == "applying").ToListAsync())
            if (ForcePayload(request)?.Server == server)
                await RefundForce(db, request, now, "server-recovery");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task RefundForce(ServerDbContext db, KabanRoundRequest request, long now, string reason)
    {
        request.Status = "refunded";
        await ChangeBalance(db, request.UserId, Guid.Empty, request.Price, Guid.NewGuid(), now, "force-refund");
        db.Add(new KabanAudit { Id = Guid.NewGuid(), Operation = request.Id, Target = request.UserId,
            Action = "ForceReturned", Detail = reason, At = now });
    }
}
