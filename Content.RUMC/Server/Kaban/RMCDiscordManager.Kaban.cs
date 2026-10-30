using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Discord;

namespace Content.Server._RMC14.Discord;

// CMU14: reuse the connected Discord.Net bot. The economy never waits for Discord while spending KBK.
public sealed partial class RMCDiscordManager
{
    public bool KabanConnected => _client?.ConnectionState == ConnectionState.Connected;

    public async Task DeliverKabanRole(KabanDiscordRole request, ulong guildId, ulong boundaryId,
        IServerDbManager db, string lease)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var options = new RequestOptions { CancelToken = timeout.Token, Timeout = 10000,
            AuditLogReason = $"RuCM KBK {request.UserId} {request.Delivery}" };
        var guild = (IGuild) await _client.Rest.GetGuildAsync(guildId, options);
        var roles = guild.Roles;
        var boundary = roles.SingleOrDefault(r => r.Id == boundaryId);
        var bot = await guild.GetCurrentUserAsync(CacheMode.AllowDownload, options);
        if (boundary == null || boundary.Id == guildId || !bot.GuildPermissions.ManageRoles
            || !roles.Any(r => bot.RoleIds.Contains(r.Id) && r.Position > boundary.Position))
            throw new InvalidOperationException("KBK Discord hierarchy/boundary is not configured safely.");
        if (!Content.Server._RuCM.Kaban.KabanEconomy.ValidRoleName(request.Name)
            || roles.Any(r => r.Id.ToString() != request.RoleId && r.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("KBK Discord role name conflicts with an existing role.");
        var user = await guild.GetUserAsync(ulong.Parse(request.DiscordId), CacheMode.AllowDownload, options);
        if (user == null)
            throw new InvalidOperationException("Linked Discord user is not in the configured guild.");
        IRole? role;
        if (request.RoleId.Length > 0)
        {
            role = roles.SingleOrDefault(r => r.Id.ToString() == request.RoleId);
            if (role == null)
                throw new InvalidOperationException("Personal role removed externally; management must reconcile it.");
        }
        else
        {
            // Persist its snowflake BEFORE renaming. A crash after creation recovers the deterministic placeholder.
            var marker = $"kbk-{request.UserId:N}";
            var placeholders = roles.Where(r => r.Name == marker).ToArray();
            if (placeholders.Length > 1)
                throw new InvalidOperationException("Ambiguous KBK role placeholders; manual reconciliation required.");
            role = placeholders.SingleOrDefault()
                ?? await guild.CreateRoleAsync(marker, GuildPermissions.None, isHoisted: false, isMentionable: false, options: options);
        }
        if (role.IsManaged || role.Id == guildId || role.Permissions.RawValue != 0)
            throw new InvalidOperationException("Personal role exceeds the permitted hierarchy or has permissions.");
        if (!await db.KabanRecordDiscord(request.UserId, request.Delivery, lease, role.Id.ToString(), false, DateTime.UtcNow.Ticks))
            return;
        role = await Content.Server._RuCM.Kaban.KabanDiscordRolePosition.PlaceBelowBoundary(
            async () => (IGuild) await _client.Rest.GetGuildAsync(guildId, options), role.Id, boundaryId, options);
        await role.ModifyAsync(p =>
        {
            p.Name = request.Name;
            p.Color = new global::Discord.Color(uint.Parse(request.Color, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            p.Permissions = GuildPermissions.None;
            p.Hoist = false;
            p.Mentionable = false;
        }, options);
        await user.AddRoleAsync(role.Id, options);
        await db.KabanRecordDiscord(request.UserId, request.Delivery, lease, role.Id.ToString(), true, DateTime.UtcNow.Ticks);
    }
}
