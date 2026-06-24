using System.Linq;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._RuMC14.RoleTests;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.RoleTests;

[AdminCommand(AdminFlags.Host)]
public sealed partial class RoleTestSetCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;

    public override string Command => "role_test_set";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Help);
            return;
        }

        var player = await _playerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (player == null)
        {
            shell.WriteError(Loc.GetString("cmd-role-test-player-not-found", ("player", args[0])));
            return;
        }

        if (!TryGetTestedJob(args[1], out var job))
        {
            shell.WriteError(Loc.GetString("cmd-role-test-job-not-found", ("job", args[1])));
            return;
        }

        if (!TryParseStatus(args[2], out var passed))
        {
            shell.WriteError(Loc.GetString("cmd-role-test-invalid-status", ("status", args[2])));
            return;
        }

        var tracker = RoleTestShared.GetTracker(RoleTestShared.GetJobTestId(job.ID));
        await _playTime.SetTimeToTrackerById(
            player.UserId,
            tracker,
            passed ? TimeSpan.FromSeconds(1) : TimeSpan.Zero);

        shell.WriteLine(Loc.GetString(
            "cmd-role_test_set-success",
            ("player", player.Username),
            ("job", job.ID),
            ("status", Loc.GetString(passed ? "role-test-admin-passed" : "role-test-admin-failed"))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _players),
                Loc.GetString("cmd-role-test-hint-player")),
            2 => CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<RoleTestQuestionPoolPrototype>().Select(pool => pool.Job.Id),
                Loc.GetString("cmd-role-test-hint-job")),
            3 => CompletionResult.FromHintOptions(
                new[] { "passed", "failed" },
                Loc.GetString("cmd-role-test-hint-status")),
            _ => CompletionResult.Empty,
        };
    }

    private bool TryGetTestedJob(string jobId, out JobPrototype job)
    {
        if (_prototypes.TryIndex<JobPrototype>(jobId, out var prototype) &&
            _prototypes.TryIndex<RoleTestQuestionPoolPrototype>(prototype.ID, out _))
        {
            job = prototype;
            return true;
        }

        job = default!;
        return false;
    }

    private static bool TryParseStatus(string value, out bool passed)
    {
        switch (value.ToLowerInvariant())
        {
            case "passed":
            case "pass":
            case "true":
            case "1":
                passed = true;
                return true;
            case "failed":
            case "fail":
            case "not-passed":
            case "false":
            case "0":
                passed = false;
                return true;
            default:
                passed = false;
                return false;
        }
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class RoleTestGetCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;

    public override string Command => "role_test_get";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        var player = await _playerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (player == null)
        {
            shell.WriteError(Loc.GetString("cmd-role-test-player-not-found", ("player", args[0])));
            return;
        }

        if (!_prototypes.TryIndex<JobPrototype>(args[1], out var job) ||
            !_prototypes.TryIndex<RoleTestQuestionPoolPrototype>(job.ID, out _))
        {
            shell.WriteError(Loc.GetString("cmd-role-test-job-not-found", ("job", args[1])));
            return;
        }

        var tracker = RoleTestShared.GetTracker(RoleTestShared.GetJobTestId(job.ID));
        var passed = await _playTime.GetPlayTimeForTrackerById(player.UserId, tracker) > TimeSpan.Zero;

        shell.WriteLine(Loc.GetString(
            "cmd-role_test_get-success",
            ("player", player.Username),
            ("job", job.ID),
            ("status", Loc.GetString(passed ? "role-test-admin-passed" : "role-test-admin-failed"))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _players),
                Loc.GetString("cmd-role-test-hint-player")),
            2 => CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<RoleTestQuestionPoolPrototype>().Select(pool => pool.Job.Id),
                Loc.GetString("cmd-role-test-hint-job")),
            _ => CompletionResult.Empty,
        };
    }
}
