using Content.Shared._RuCM.Discord;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.LinkAccount;

public sealed partial class LinkAccountSystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    private void InitializeDiscordRequirement()
        => SubscribeNetworkEvent<DiscordLinkRequirementEvent>(ev =>
            _ui.GetUIController<LinkAccountUIController>().ShowDiscordRequirement(ev.Required));
}
