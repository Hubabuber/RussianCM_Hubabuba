using Content.Client._RMC14.LinkAccount;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class MiscTab
{
    [Dependency] private IPlayerManager _discordLinkPlayers = default!;

    private void InitializeRuCMDiscordSettings()
    {
        DiscordLinkButton.OnPressed += _ => UserInterfaceManager.GetUIController<LinkAccountUIController>().ToggleWindow();
        RefreshRuCMDiscordSettings();
    }

    private void RefreshRuCMDiscordSettings()
    {
        var connected = _discordLinkPlayers.LocalSession != null;
        DiscordLinkButton.Disabled = !connected;
        DiscordLinkOffline.Visible = !connected;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        RefreshRuCMDiscordSettings();
    }
}
