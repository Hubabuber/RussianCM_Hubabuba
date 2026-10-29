using Content.Shared._RMC14.LinkAccount;

namespace Content.Client._RMC14.LinkAccount;

public sealed partial class LinkAccountSystem : EntitySystem
{
    public event Action<SharedRMCDisplayLobbyMessageEvent>? LobbyMessageReceived;

    public override void Initialize()
    {
        SubscribeNetworkEvent<SharedRMCDisplayLobbyMessageEvent>(OnDisplayLobbyMessage);
        InitializeDiscordRequirement(); // CMU14
    }

    private void OnDisplayLobbyMessage(SharedRMCDisplayLobbyMessageEvent ev)
    {
        LobbyMessageReceived?.Invoke(ev);
    }
}
