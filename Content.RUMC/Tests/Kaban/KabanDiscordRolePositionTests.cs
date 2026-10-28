using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Content.Server._RuCM.Kaban;
using Discord;

namespace Content.IntegrationTests._RuCM.Kaban;

public sealed class KabanDiscordRolePositionTests
{
    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> call) where T : class
    {
        var proxy = DispatchProxy.Create<T, DiscordLinkPanelTests.InterfaceProxy>();
        ((DiscordLinkPanelTests.InterfaceProxy) (object) proxy).Call = call;
        return proxy;
    }

    private sealed class Role(ulong id, int position, uint color = 0)
    {
        public ulong Id = id;
        public int Position = position;
        public uint Color = color;
        public bool Managed;
        public GuildPermissions Permissions = GuildPermissions.None;

        public IRole Snapshot()
        {
            var position = Position;
            return Proxy<IRole>((method, _) => method.Name switch
            {
                "get_Id" => Id,
                "get_Position" => position,
                "get_IsManaged" => Managed,
                "get_Permissions" => Permissions,
                "get_Color" => new global::Discord.Color(Color),
                _ => throw new NotSupportedException(method.Name),
            });
        }
    }

    private sealed class Guild
    {
        public readonly Role Personal = new(4, 1, 0xAC00FF);
        public readonly Role Boundary = new(3, 10);
        public readonly Role Bot = new(2, 11);
        public readonly Role Ordinary = new(5, 8, 0xCCCCCC);
        public bool CanManage = true;
        public bool ReorderFailure;
        public bool BreakBoundaryAfterMove;
        public int Loads;
        public int Reorders;
        public int Target;

        public Task<IGuild> Reload()
        {
            Loads++;
            var roles = new[] { Personal, Boundary, Bot, Ordinary }.Select(r => r.Snapshot()).ToArray();
            var bot = Proxy<IGuildUser>((method, _) => method.Name switch
            {
                "get_RoleIds" => (IReadOnlyCollection<ulong>) new[] { Bot.Id },
                "get_GuildPermissions" => CanManage ? new GuildPermissions(manageRoles: true) : GuildPermissions.None,
                _ => throw new NotSupportedException(method.Name),
            });
            var guild = Proxy<IGuild>((method, args) => method.Name switch
            {
                "get_Id" => 1UL,
                "get_Roles" => (IReadOnlyCollection<IRole>) roles,
                "GetCurrentUserAsync" => Task.FromResult(bot),
                "ReorderRolesAsync" => Reorder((IEnumerable<ReorderRoleProperties>) args![0]!),
                _ => throw new NotSupportedException(method.Name),
            });
            return Task.FromResult(guild);
        }

        private Task Reorder(IEnumerable<ReorderRoleProperties> changes)
        {
            var change = changes.Single();
            Assert.That(change.Id, Is.EqualTo(Personal.Id), "Do not reorder the boundary, bot or other members' roles.");
            Reorders++;
            Target = change.Position;
            if (ReorderFailure) throw new IOException("Test: Discord rejected the reorder.");
            Personal.Position = Target;
            if (BreakBoundaryAfterMove) Boundary.Position = Target;
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task BottomRoleMovesUnderFreshBoundaryAboveOrdinaryColoredRole()
    {
        var guild = new Guild();
        var role = await KabanDiscordRolePosition.PlaceBelowBoundary(guild.Reload, 4, 3, new RequestOptions());
        Assert.That(guild.Target, Is.EqualTo(9));
        Assert.That(role.Position, Is.EqualTo(9));
        Assert.That(role.Position, Is.GreaterThan(guild.Ordinary.Position));
        Assert.That(role.Color.RawValue, Is.EqualTo(0xAC00FF));
        Assert.That(guild.Boundary.Position, Is.EqualTo(10));
        Assert.That(guild.Loads, Is.EqualTo(2), "Re-read roles after the move.");
    }

    [Test]
    public async Task AlreadyPositionedRoleIsNotMovedAgain()
    {
        var guild = new Guild();
        guild.Personal.Position = 9;
        await KabanDiscordRolePosition.PlaceBelowBoundary(guild.Reload, 4, 3, new RequestOptions());
        Assert.That(guild.Reorders, Is.Zero);
    }

    [TestCase("managed")]
    [TestCase("permissions")]
    [TestCase("above-boundary")]
    [TestCase("bot-too-low")]
    [TestCase("no-manage-roles")]
    [TestCase("missing-boundary")]
    public void UnsafeHierarchyCannotBeReordered(string failure)
    {
        var guild = new Guild();
        switch (failure)
        {
            case "managed": guild.Personal.Managed = true; break;
            case "permissions": guild.Personal.Permissions = new GuildPermissions(administrator: true); break;
            case "above-boundary": guild.Personal.Position = 10; break;
            case "bot-too-low": guild.Bot.Position = 9; break;
            case "no-manage-roles": guild.CanManage = false; break;
        }
        Assert.ThrowsAsync<InvalidOperationException>(() => KabanDiscordRolePosition.PlaceBelowBoundary(
            guild.Reload, 4, failure == "missing-boundary" ? 100UL : 3, new RequestOptions()));
        Assert.That(guild.Reorders, Is.Zero);
    }

    [Test]
    public void ReorderFailureDoesNotReportSuccessfulPositioning()
    {
        var guild = new Guild { ReorderFailure = true };
        Assert.ThrowsAsync<IOException>(() => KabanDiscordRolePosition.PlaceBelowBoundary(guild.Reload, 4, 3, new RequestOptions()));
        Assert.That(guild.Loads, Is.EqualTo(1));
    }

    [Test]
    public void BoundaryChangedDuringReorderFailsTheFinalCheck()
    {
        var guild = new Guild { BreakBoundaryAfterMove = true };
        Assert.ThrowsAsync<InvalidOperationException>(() => KabanDiscordRolePosition.PlaceBelowBoundary(guild.Reload, 4, 3, new RequestOptions()));
        Assert.That(guild.Loads, Is.EqualTo(2));
    }
}
