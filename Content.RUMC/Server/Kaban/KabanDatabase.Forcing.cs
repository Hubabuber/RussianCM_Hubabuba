using System.Threading.Tasks;
using Content.Server._RuCM.Kaban;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<KabanRoundRequest?> KabanClaimForce(string server, string attempt, long now);
    Task KabanFinishForce(string server, Guid id, string attempt, bool used, long now, string reason);
    Task KabanRecoverForce(string server, long now);
}

public sealed partial class ServerDbManager
{
    public Task<KabanRoundRequest?> KabanClaimForce(string server, string attempt, long now)
        => RunDbCommand(() => _db.KabanClaimForce(server, attempt, now));
    public Task KabanFinishForce(string server, Guid id, string attempt, bool used, long now, string reason)
        => RunDbCommand(() => _db.KabanFinishForce(server, id, attempt, used, now, reason));
    public Task KabanRecoverForce(string server, long now)
        => RunDbCommand(() => _db.KabanRecoverForce(server, now));
}

public abstract partial class ServerDbBase
{
    public async Task<KabanRoundRequest?> KabanClaimForce(string server, string attempt, long now)
    {
        await using var guard = await GetDb();
        return await KabanEconomy.ClaimForce(guard.DbContext, server, attempt, now);
    }
    public async Task KabanFinishForce(string server, Guid id, string attempt, bool used, long now, string reason)
    {
        await using var guard = await GetDb();
        await KabanEconomy.FinishForce(guard.DbContext, server, id, attempt, used, now, reason);
    }
    public async Task KabanRecoverForce(string server, long now)
    {
        await using var guard = await GetDb();
        await KabanEconomy.RecoverForce(guard.DbContext, server, now);
    }
}
