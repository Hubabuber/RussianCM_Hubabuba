using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared._RMC14.CCVar;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.Round;

public sealed partial class AuVoteRuleSystem : GameRuleSystem<AuVoteRuleComponent>
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private bool _pausedForMinimumPlayers;
    private bool _waitingForMinimumPlayers;
    private bool _voteStartDelayed;
    private int _voteDelayGen;
    private bool _startedForLobby; // CMU14: simultaneous connection callbacks must not restart a claimed sequence.

    // Only keep the persistent system trigger and dependency injection
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
    }


    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _startedForLobby = false;
        // Delay only covers the post-boot connect window; restarts with enough players re-run at once.
        _voteDelayGen++;
        var gen = _voteDelayGen;
        _voteStartDelayed = false;
        var delay = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteStartDelay));
        if (delay > TimeSpan.Zero
                && _playerManager.PlayerCount < _cfg.GetCVar(CCVars.VoteStartDelayMinPlayers))
        {
            _voteStartDelayed = true;
            Timer.Spawn(delay, () =>
            {
                if (gen != _voteDelayGen)
                    return;

                _voteStartDelayed = false;
                TryStartVoteSequence();
            });
            PauseForMinimumPlayers();
        }
        else
            Timer.Spawn(0, () =>
            {
                if (gen == _voteDelayGen)
                    TryStartVoteSequence(); // CMU14: all cleanup subscribers must finish before a new paid reservation starts.
            });
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (!_waitingForMinimumPlayers)
            return;

        // PlayerStatusChanged can be raised before PlayerCount includes the newly connected session.
        Timer.Spawn(0, TryStartVoteSequence);
    }

    private void TryStartVoteSequence()
    {
        if (_voteStartDelayed || _startedForLobby)
            return;

        if (!AuLobbyVoteGate.ShouldStartVoteSequence(
                GameTicker.LobbyEnabled,
                GameTicker.RunLevel,
                _playerManager.PlayerCount,
                _cfg.GetCVar(RMCCVars.RMCLobbyMinimumPlayers)))
        {
            _waitingForMinimumPlayers = GameTicker.LobbyEnabled &&
                                        GameTicker.RunLevel == GameRunLevel.PreRoundLobby;
            if (_waitingForMinimumPlayers)
                PauseForMinimumPlayers();
            return;
        }

        _waitingForMinimumPlayers = false;
        _startedForLobby = true;
        var voteManagerSystem = _entityManager.System<AuRoundSystem>();
        RestartCountdownAfterMinimumPlayers();
        voteManagerSystem.StartVoteSequence(() => { }); // CMU14: restore the selection window before claiming a queued round.
    }

    private void PauseForMinimumPlayers()
    {
        if (_pausedForMinimumPlayers || GameTicker.Paused)
            return;

        GameTicker.PauseStart();
        _pausedForMinimumPlayers = !_cfg.GetCVar(RMCCVars.RMCLobbyStartPaused);
    }

    private void RestartCountdownAfterMinimumPlayers()
    {
        if (!_pausedForMinimumPlayers)
            return;

        _pausedForMinimumPlayers = false;
        GameTicker.RestartLobbyCountdown();
    }
}
