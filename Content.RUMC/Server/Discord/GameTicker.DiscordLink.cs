using Content.Server._RuCM.Discord;
using Robust.Shared.Player;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private DiscordLinkGateSystem DiscordGate => EntityManager.System<DiscordLinkGateSystem>();

    internal void JoinDiscordLinkLobby(ICommonSession player)
    {
        // Detach control only: the character and its owning mind survive while the account is linked.
        _playerManager.SetAttachedEntity(player, null);
        PlayerJoinLobby(player);
    }

    internal void ResumeAfterDiscordLink(ICommonSession player)
    {
        if (!DiscordGate.CanPlay(player) || !_userDb.IsLoadComplete(player)) return;
        if (_mind.TryGetMind(player.UserId, out _, out var mind))
        {
            if (mind.CurrentEntity is { } body && !Deleted(body) && _playerManager.SetAttachedEntity(player, body))
                PlayerJoinGame(player);
            else
                JoinAsObserver(player);
        }
        else if (!LobbyEnabled)
            SpawnPlayer(player, EntityUid.Invalid);
    }
}
