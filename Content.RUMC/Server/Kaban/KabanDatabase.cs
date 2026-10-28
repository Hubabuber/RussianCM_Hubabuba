using System.Linq;
using System.Threading.Tasks;
using Content.Server._RuCM.Kaban;
using Content.Shared._RuCM.Kaban;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

// CMU14: same configured database, context lifetime, worker pool and metrics as existing account features.
public partial interface IServerDbManager
{
    Task<string> KabanExecute(KabanPolicy policy, KabanRequest request, DateTime now);
    Task<KabanState> KabanView(KabanPolicy policy);
    Task<List<KabanToken>> KabanReservations(string server);
    Task<List<KabanVoice>> KabanVoices();
    Task KabanImportLegacyVoices(string[] speakers);
    Task KabanSettle(string server, Guid reservation, bool used, string job, bool command, long now);
}

public sealed partial class ServerDbManager
{
    public Task<string> KabanExecute(KabanPolicy policy, KabanRequest request, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.KabanExecute(policy, request, now));
    }
    public Task<KabanState> KabanView(KabanPolicy policy)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.KabanView(policy));
    }
    public Task<List<KabanToken>> KabanReservations(string server)
        => RunDbCommand(() => _db.KabanReservations(server));
    public Task<List<KabanVoice>> KabanVoices()
        => RunDbCommand(() => _db.KabanVoices());
    public Task KabanImportLegacyVoices(string[] speakers)
        => RunDbCommand(() => _db.KabanImportLegacyVoices(speakers));
    public Task KabanSettle(string server, Guid reservation, bool used, string job, bool command, long now)
        => RunDbCommand(() => _db.KabanSettle(server, reservation, used, job, command, now));
}

public abstract partial class ServerDbBase
{
    public async Task<string> KabanExecute(KabanPolicy policy, KabanRequest request, DateTime now)
    {
        await using var guard = await GetDb();
        return await KabanEconomy.Execute(guard.DbContext, policy, request, now);
    }
    public async Task<List<KabanToken>> KabanReservations(string server)
    {
        await using var guard = await GetDb();
        return await guard.DbContext.Set<KabanToken>().AsNoTracking()
            .Where(t => t.Status == "reserved" && t.Server == server).ToListAsync();
    }
    public async Task<List<KabanVoice>> KabanVoices()
    {
        await using var guard = await GetDb();
        var db = guard.DbContext;
        return await (from claim in db.Set<KabanVoiceClaim>().AsNoTracking()
            join voice in db.Set<KabanVoice>().AsNoTracking() on claim.Speaker equals voice.Speaker into current
            from voice in current.DefaultIfEmpty()
            select new KabanVoice { UserId = claim.UserId, Speaker = claim.Speaker,
                Status = claim.UserId == Guid.Empty ? "legacy" : voice == null ? "retired" : voice.Status }).ToListAsync();
    }
    public async Task KabanImportLegacyVoices(string[] speakers)
    {
        await using var guard = await GetDb();
        var db = guard.DbContext;
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1");
        const string marker = "@legacy-initialized";
        if (await db.Set<KabanVoiceClaim>().AnyAsync(c => c.Speaker == marker))
            return;
        var claimed = (await db.Set<KabanVoiceClaim>().Select(c => c.Speaker).ToArrayAsync()).ToHashSet(StringComparer.Ordinal);
        foreach (var speaker in speakers.Distinct(StringComparer.Ordinal))
            if (Content.Shared.Corvax.TTS.CustomTTSVoice.IsValidSpeakerName(speaker) && !claimed.Contains(speaker))
                db.Add(new KabanVoiceClaim { Speaker = speaker, UserId = Guid.Empty });
        db.Add(new KabanVoiceClaim { Speaker = marker, UserId = Guid.Empty });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    public async Task KabanSettle(string server, Guid reservation, bool used, string job, bool command, long now)
    {
        await using var guard = await GetDb();
        await KabanEconomy.Settle(guard.DbContext, server, reservation, used, job, command, now);
    }
    public async Task<KabanState> KabanView(KabanPolicy policy)
    {
        await using var guard = await GetDb();
        var db = guard.DbContext;
        var view = new KabanState
        {
            Viewer = policy.Actor, Manager = policy.Manager, Senior = policy.Senior,
            PrioritiesEnabled = policy.Priorities, Lobby = policy.Lobby,
            ForcingEnabled = policy.Forcing, ForceOptions = policy.ForceOptions?.ToList() ?? [],
        };
        var wallet = await db.Set<KabanWallet>().AsNoTracking().SingleOrDefaultAsync(w => w.UserId == policy.Actor);
        view.Balance = wallet?.Balance ?? 0;
        view.Earned = wallet?.Earned ?? 0;
        foreach (var product in await db.Set<KabanProduct>().AsNoTracking().OrderBy(p => p.Price).ToListAsync())
        {
            var blocker = KabanEconomy.Blocker(product.Id, policy);
            view.Products.Add(new(product.Id, product.Price, product.Enabled && blocker.Length == 0,
                product.Enabled ? blocker : "cmu-kaban-product-unavailable"));
        }
        var now = DateTime.UtcNow.Ticks;
        foreach (var token in await db.Set<KabanToken>().AsNoTracking()
                     .Where(t => t.UserId == policy.Actor && t.Status != "spent" && t.Expires > now).OrderBy(t => t.Expires).Take(100).ToListAsync())
            view.Items.Add(new(token.Id, "token", "", token.Expires, token.Status, token.Multiplier, token.Flexible,
                System.Text.Json.JsonSerializer.Deserialize<string[]>(token.Jobs) ?? []));
        foreach (var voice in await db.Set<KabanVoice>().AsNoTracking()
                     .Where(v => policy.Manager || v.UserId == policy.Actor)
                     .OrderBy(v => v.Status != "pending").ThenBy(v => v.UserId).Take(100).ToListAsync())
            view.Items.Add(new(voice.UserId, "voice", voice.Speaker, 0, voice.Status, Detail: voice.Consent));
        foreach (var role in await db.Set<KabanDiscordRole>().AsNoTracking()
                     .Where(v => policy.Manager || v.UserId == policy.Actor)
                     .OrderBy(v => v.Status != "pending").ThenBy(v => v.UserId).Take(100).ToListAsync())
            view.Items.Add(new(role.UserId, "discord", $"{role.Name} #{role.Color}", 0, role.Status));
        var pending = (await db.Set<KabanRoundRequest>().AsNoTracking()
            .Where(q => q.Status == "queued" || q.Status == "applying").OrderBy(q => q.At).ThenBy(q => q.Id).ToListAsync())
            .Where(q => KabanEconomy.ForcePayload(q)?.Server == policy.Server).ToList();
        var requests = await db.Set<KabanRoundRequest>().AsNoTracking()
                     .Where(q => q.UserId == policy.Actor || policy.Manager)
                     .OrderBy(q => q.Status != "queued" && q.Status != "applying").ThenByDescending(q => q.At).Take(100).ToListAsync();
        var owners = requests.Select(q => q.UserId).Distinct().ToArray();
        var names = await db.Player.AsNoTracking().Where(p => owners.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.LastSeenUserName);
        foreach (var queue in requests)
            view.Items.Add(new(queue.Id, "force", queue.ProductId, 0, queue.Status,
                Detail: KabanEconomy.ForcePayload(queue)?.Choice.Encode() ?? "",
                Position: pending.FindIndex(q => q.Id == queue.Id) + 1, OwnerName: names.GetValueOrDefault(queue.UserId, "")));
        foreach (var entry in await db.Set<KabanLedger>().AsNoTracking().Where(l => l.UserId == policy.Actor)
                     .OrderByDescending(l => l.At).Take(100).ToListAsync())
            view.History.Add(new(entry.Id, entry.At, entry.Reason, entry.Delta));
        if (policy.Manager)
        {
            foreach (var audit in await db.Set<KabanAudit>().AsNoTracking().OrderByDescending(a => a.At).Take(100).ToListAsync())
                view.Audit.Add(new(audit.Id, audit.At, $"{audit.Actor} → {audit.Target}: {audit.Action} {audit.Detail}", 0));
            foreach (var account in await db.Set<KabanWallet>().AsNoTracking().OrderBy(w => w.UserId).Take(100).ToListAsync())
                view.Wallets.Add(new(account.UserId, account.Balance, account.Earned));
        }
        var accountIds = view.Wallets.Select(w => w.UserId).Append(policy.Actor).Distinct().ToArray();
        foreach (var account in await db.Player.AsNoTracking().Where(p => accountIds.Contains(p.UserId)).ToListAsync())
            view.Accounts[account.UserId] = account.LastSeenUserName;
        return view;
    }
}
