using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RuMC14.RoleTests;

[AnyCommand]
public sealed partial class RoleTestsCommand : LocalizedEntityCommands
{
    [Dependency] private EuiManager _eui = default!;

    public override string Command => "roletests";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var testId = args.Length > 0 ? args[0] : null;
        _eui.OpenEui(new RoleTestEui(testId), shell.Player);
    }
}
