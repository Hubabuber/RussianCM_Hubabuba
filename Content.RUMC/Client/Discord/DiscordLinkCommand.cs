using Content.Client._RMC14.LinkAccount;
using Content.Shared.Administration;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.Localization;

namespace Content.Client._RuCM.Discord;

[AnyCommand]
public sealed class DiscordLinkCommand : LocalizedCommands
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public override string Command => "discordlink";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0) { shell.WriteError(Help); return; }
        if (_players.LocalSession == null)
        {
            shell.WriteError(Loc.GetString("rucm-discord-link-connect-first"));
            return;
        }
        _ui.GetUIController<LinkAccountUIController>().ToggleWindow();
    }
}
