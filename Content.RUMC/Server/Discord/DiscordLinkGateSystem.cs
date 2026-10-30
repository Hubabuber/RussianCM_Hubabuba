using System.Linq;
using Content.Server._RMC14.LinkAccount;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Database;
using Content.Shared._RMC14.LinkAccount;
using Content.Shared._RuCM.Discord;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RuCM.Discord;

/// <summary>Admission uses the persisted account link, independently of the bot's current connection.</summary>
public sealed class DiscordLinkGateSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private LinkAccountManager _links = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<NetUserId> _waiting = new();
    private readonly Dictionary<NetUserId, TimeSpan> _notices = new();
    private readonly HashSet<NetUserId> _prompted = new();
    private TimeSpan _nextCheck;

    public bool Required => _config.GetCVar(DiscordLinkingCVars.Required);
    public bool CanPlay(ICommonSession player) => !Required || _links.GetConnectedPatron(player)?.Linked == true;

    public override void Initialize()
    {
        Subs.CVar(_config, DiscordLinkingCVars.Required, OnRequiredChanged, true);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnAttached);
        _players.PlayerStatusChanged += OnStatusChanged;
        _links.PatronUpdated += OnPatronUpdated;
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnStatusChanged;
        _links.PatronUpdated -= OnPatronUpdated;
        base.Shutdown();
    }

    public bool EnsureCanPlay(ICommonSession player)
    {
        if (CanPlay(player)) return true;
        Notify(player);
        return false;
    }

    public void WaitForLink(ICommonSession player)
    {
        _waiting.Add(player.UserId);
        _ticker.JoinDiscordLinkLobby(player);
        _nextCheck = TimeSpan.Zero;
    }

    private void OnRequiredChanged(bool required)
    {
        _nextCheck = TimeSpan.Zero;
        if (!required) return;
        foreach (var player in _players.Sessions.ToArray())
            if (player.Status == SessionStatus.InGame && !CanPlay(player))
                WaitForLink(player);
    }

    private void OnAttached(PlayerAttachedEvent ev)
    {
        // Covers direct body transfers and administrative/ghost-role paths as well as the normal join buttons.
        if (!CanPlay(ev.Player)) WaitForLink(ev.Player);
    }

    private void OnPatronUpdated((NetUserId Id, SharedRMCPatronFull Patron) update)
    {
        if (Required && !update.Patron.Linked && _players.TryGetSessionById(update.Id, out var player)
            && player.Status == SessionStatus.InGame && !_waiting.Contains(update.Id))
            WaitForLink(player);
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs ev)
    {
        if (ev.NewStatus != SessionStatus.Disconnected) return;
        _waiting.Remove(ev.Session.UserId);
        _notices.Remove(ev.Session.UserId);
        _prompted.Remove(ev.Session.UserId);
    }

    private void Notify(ICommonSession player)
    {
        if (player.Status != SessionStatus.InGame) return;
        if (_notices.TryGetValue(player.UserId, out var previous) && _timing.RealTime < previous + TimeSpan.FromSeconds(2))
            return;
        _notices[player.UserId] = _timing.RealTime;
        var loaded = _userDb.IsLoadComplete(player);
        _chat.DispatchServerMessage(player, Loc.GetString(loaded ? "rucm-discord-link-required" : "rucm-discord-link-checking"));
        if (loaded)
        {
            _prompted.Add(player.UserId);
            RaiseNetworkEvent(new DiscordLinkRequirementEvent { Required = true }, player.Channel);
        }
    }

    public override void Update(float frameTime)
    {
        if (_waiting.Count == 0 || _timing.RealTime < _nextCheck) return;
        _nextCheck = _timing.RealTime + TimeSpan.FromSeconds(0.5);
        foreach (var id in _waiting.ToArray())
        {
            if (!_players.TryGetSessionById(id, out var player) || player.Status != SessionStatus.InGame)
                continue;
            if (!_userDb.IsLoadComplete(player)) continue;
            if (!CanPlay(player))
            {
                if (!_prompted.Contains(id)) Notify(player);
                continue;
            }
            _waiting.Remove(id);
            _notices.Remove(id);
            _prompted.Remove(id);
            RaiseNetworkEvent(new DiscordLinkRequirementEvent { Required = false }, player.Channel);
            _ticker.ResumeAfterDiscordLink(player);
        }
    }
}
