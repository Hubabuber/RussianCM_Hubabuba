using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Content.Server._RuCM.Kaban;

public static class KabanDiscordRolePosition
{
    /// <summary>Refresh after creation (which shifts positions), move only our role, then recheck the boundary.</summary>
    public static async Task<IRole> PlaceBelowBoundary(Func<Task<IGuild>> reload,
        ulong roleId, ulong boundaryId, RequestOptions options)
    {
        var guild = await reload();
        var (role, boundary) = await Validate(guild, roleId, boundaryId, options);
        if (role.Position != boundary.Position - 1)
            await guild.ReorderRolesAsync([new ReorderRoleProperties(role.Id, boundary.Position - 1)], options);
        // Do not use a stale REST role snapshot to decide whether delivery succeeded.
        guild = await reload();
        (role, _) = await Validate(guild, roleId, boundaryId, options);
        return role;
    }

    private static async Task<(IRole Role, IRole Boundary)> Validate(IGuild guild,
        ulong roleId, ulong boundaryId, RequestOptions options)
    {
        var roles = guild.Roles;
        var role = roles.SingleOrDefault(r => r.Id == roleId);
        var boundary = roles.SingleOrDefault(r => r.Id == boundaryId);
        var bot = await guild.GetCurrentUserAsync(CacheMode.AllowDownload, options);
        if (boundary == null || boundary.Id == guild.Id || !bot.GuildPermissions.ManageRoles
            || !roles.Any(r => bot.RoleIds.Contains(r.Id) && r.Position > boundary.Position))
            throw new InvalidOperationException("KBK Discord hierarchy/boundary is not configured safely.");
        if (role == null || role.IsManaged || role.Id == guild.Id || role.Id == boundary.Id
            || role.Position >= boundary.Position || role.Permissions.RawValue != 0 || boundary.Position < 2)
            throw new InvalidOperationException("Personal role exceeds the permitted hierarchy or has permissions.");
        return (role, boundary);
    }
}
