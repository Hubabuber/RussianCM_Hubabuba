using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class RMCPatronPersistence
{
    public static async Task<bool> SetTierAsync(
        ServerDbContext db,
        Guid playerId,
        int tierId,
        CancellationToken cancel = default)
    {
        var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO rmc_patrons (player_id, tier_id)
            VALUES ({playerId}, {tierId})
            ON CONFLICT (player_id) DO UPDATE
            SET tier_id = excluded.tier_id
            WHERE rmc_patrons.tier_id <> excluded.tier_id
            """, cancel);
        return changed > 0;
    }

    public static async Task<bool> RemoveAsync(
        ServerDbContext db,
        Guid playerId,
        CancellationToken cancel = default)
    {
        var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM rmc_patrons
            WHERE player_id = {playerId}
            """, cancel);
        return changed > 0;
    }
}
