using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot;

public static class BoostyTierSeeder
{
    private sealed record TierSeed(
        string Name,
        ulong DiscordRole,
        int Priority,
        bool ShowOnCredits,
        bool GhostColor,
        bool NamedItems,
        bool Figurines,
        bool LobbyMessage,
        bool RoundEndShoutout);

    private static readonly TierSeed[] Tiers =
    [
        new("\u041a\u043e\u043b\u043e\u043d\u0438\u0441\u0442", 1512833966292471868UL, 7, true, false, false, false, false, false),
        new("\u041d\u043e\u0432\u043e\u0431\u0440\u0430\u043d\u0435\u0446", 1512833998106132621UL, 6, true, true, false, false, false, false),
        new("\u0411\u043e\u0435\u0446", 1512834040233721906UL, 5, true, true, true, false, false, false),
        new("\u0428\u0442\u0443\u0440\u043c\u043e\u0432\u0438\u043a", 1512834069350846604UL, 4, true, true, true, true, false, false),
        new("\u0420\u0430\u0437\u0432\u0435\u0434\u0447\u0438\u043a", 1512834091488120973UL, 3, true, true, true, true, true, false),
        new("\u041a\u043e\u043c\u0430\u043d\u0434\u0438\u0440 \u044f\u0447\u0435\u0439\u043a\u0438", 1512834125331959919UL, 2, true, true, true, true, true, true),
        new("\u041b\u0438\u0434\u0435\u0440 \u0432\u043e\u0441\u0441\u0442\u0430\u043d\u0438\u044f", 1512834158966079682UL, 1, true, true, true, true, true, true),
    ];

    public static async Task Seed(ServerDbContext db)
    {
        foreach (var seed in Tiers)
        {
            var tier = await db.RMCPatronTiers.FirstOrDefaultAsync(t => t.DiscordRole == seed.DiscordRole);
            if (tier == null)
            {
                tier = new RMCPatronTier { DiscordRole = seed.DiscordRole };
                db.RMCPatronTiers.Add(tier);
            }

            tier.Name = seed.Name;
            tier.Priority = seed.Priority;
            tier.ShowOnCredits = seed.ShowOnCredits;
            tier.GhostColor = seed.GhostColor;
            tier.NamedItems = seed.NamedItems;
            tier.Figurines = seed.Figurines;
            tier.LobbyMessage = seed.LobbyMessage;
            tier.RoundEndShoutout = seed.RoundEndShoutout;
        }

        await db.SaveChangesAsync();
    }

    public static async Task GrantTestTier(ServerDbContext db, string playerNameOrId, string tierName)
    {
        var player = await db.Player.FirstOrDefaultAsync(p => p.UserId.ToString() == playerNameOrId) ??
                     await db.Player.FirstOrDefaultAsync(p => p.LastSeenUserName == playerNameOrId);
        if (player == null)
            throw new ArgumentException($"Player '{playerNameOrId}' was not found.");

        var tier = await db.RMCPatronTiers.FirstOrDefaultAsync(t => t.Name == tierName);
        if (tier == null)
            throw new ArgumentException($"Sponsor tier '{tierName}' was not found.");

        var patron = await db.RMCPatrons.FirstOrDefaultAsync(p => p.PlayerId == player.UserId);
        if (patron == null)
        {
            patron = new RMCPatron { PlayerId = player.UserId };
            db.RMCPatrons.Add(patron);
        }

        patron.TierId = tier.Id;
        await db.SaveChangesAsync();
    }

    public static async Task PrintTiers(ServerDbContext db)
    {
        var tiers = await db.RMCPatronTiers
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync();

        foreach (var tier in tiers)
        {
            Console.WriteLine($"{tier.Priority}: {tier.Name} role={tier.DiscordRole} credits={tier.ShowOnCredits} ghost={tier.GhostColor} named={tier.NamedItems} figurines={tier.Figurines} lobby={tier.LobbyMessage} shoutout={tier.RoundEndShoutout}");
        }
    }

    public static async Task PrintPatrons(ServerDbContext db)
    {
        var patrons = await db.RMCPatrons
            .Include(p => p.Player)
            .Include(p => p.Tier)
            .OrderBy(p => p.Tier.Priority)
            .ThenBy(p => p.Player.LastSeenUserName)
            .ToListAsync();

        foreach (var patron in patrons)
        {
            Console.WriteLine($"{patron.Player.LastSeenUserName} {patron.PlayerId} -> {patron.Tier.Name}");
        }
    }
}
