using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Log;
using Robust.Shared.Console;
using Content.Shared.Administration;
using static Content.Server.GameTicking.GameTicker;

namespace Content.Server._RuCM.Kaban;

// CMU14: database callbacks enter the simulation only through the completion queue.
public sealed partial class KabanSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly Content.Server._RMC14.Discord.RMCDiscordManager _discord = default!;
    [Dependency] private readonly Robust.Server.Player.IPlayerManager _players = default!;
    private readonly ConcurrentQueue<Action> _completed = new();
    private readonly HashSet<Guid> _busy = [];
    private readonly Dictionary<Guid, DateTime> _last = new();
    private readonly Dictionary<Guid, Armed> _armed = new();
    private readonly Dictionary<Guid, string> _participated = new();
    private readonly Dictionary<string, KabanVoice> _voices = new(StringComparer.Ordinal);
    private readonly List<KabanEui> _open = [];
    private readonly Queue<(Guid Id, bool Used, string Job)> _settlements = new();
    private Guid _lobby = Guid.NewGuid();
    private bool _ready;
    private bool _loading;
    private bool _settling;
    private DateTime _retry;
    private bool _lottery;
    private bool _delivering;
    private DateTime _nextDiscord;
    private DateTime _nextVoices;
    private bool _refreshingVoices;
    private ISawmill _log = default!;

    private sealed record Armed(Guid Token, Guid Reservation, int Weight, long Expires, string[] Jobs);

    public bool Enabled => _cfg.GetCVar(KabanCVars.Enabled);
    public bool HasArmedPriorities => _armed.Count > 0 && Enabled && _ready && _cfg.GetCVar(KabanCVars.Priorities);
    public bool IsManager(Guid actor) => Ids(_cfg.GetCVar(KabanCVars.Managers)).Contains(actor) || IsSenior(actor);
    private bool IsSenior(Guid actor) => Ids(_cfg.GetCVar(KabanCVars.SeniorManagers)).Contains(actor);
    private static HashSet<Guid> Ids(string value)
        => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(v => Guid.TryParse(v, out _)).Select(Guid.Parse).ToHashSet();

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("rucm.kaban");
        SubscribeNetworkEvent<KabanOpenEvent>((_, args) => Open(args.SenderSession));
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevel);
        SubscribeLocalEvent<PostGameMapLoad>(ev => _forceLoadedMaps.Add(ev.GameMap.ID));
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            CancelForcing("round-restart");
            ReturnReservations(false);
            _lobby = Guid.NewGuid();
            _lottery = false;
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        while (_completed.TryDequeue(out var callback))
        {
            try { callback(); }
            catch (Exception e) { _log.Error($"Kaban completion failed; game continues: {e}"); }
        }
        UpdateForcing();
        if (!_ready && !_loading && DateTime.UtcNow >= _retry)
        {
            _loading = true;
            _ = Recover();
        }
        if (!Enabled && _armed.Count > 0)
            ReturnReservations(false);
        if (_ready && !_refreshingVoices && DateTime.UtcNow >= _nextVoices)
        {
            _refreshingVoices = true;
            _nextVoices = DateTime.UtcNow.AddMinutes(1);
            _ = RefreshVoices();
        }
        if (!_settling && _settlements.TryPeek(out var settlement) && DateTime.UtcNow >= _retry)
        {
            _settling = true;
            _ = Settle(settlement);
        }
        if (Enabled && _ready && _cfg.GetCVar(KabanCVars.Discord) && !_delivering
            && DateTime.UtcNow >= _nextDiscord && _discord.KabanConnected
            && ulong.TryParse(_cfg.GetCVar(KabanCVars.DiscordGuild), out var guild)
            && ulong.TryParse(_cfg.GetCVar(KabanCVars.DiscordBoundaryRole), out var boundary))
        {
            _delivering = true;
            _nextDiscord = DateTime.UtcNow.AddMinutes(1);
            _ = DeliverDiscord(guild, boundary);
        }
    }

    private async Task Recover()
    {
        try
        {
            await _db.KabanRecoverForce(_cfg.GetCVar(KabanCVars.ServerId), DateTime.UtcNow.Ticks);
            var reservations = await _db.KabanReservations(_cfg.GetCVar(KabanCVars.ServerId));
            foreach (var token in reservations)
                await _db.KabanSettle(token.Server, token.Reservation, false, "recovery", false, DateTime.UtcNow.Ticks);
            var voices = await _db.KabanVoices();
            _completed.Enqueue(() =>
            {
                _voices.Clear();
                foreach (var voice in voices)
                    _voices[voice.Speaker] = voice;
                _ready = true;
                _loading = false;
            });
        }
        catch (Exception e)
        {
            _log.Error($"Kaban storage initialization failed; economy unavailable, game continues: {e}");
            _completed.Enqueue(() => { _loading = false; _retry = DateTime.UtcNow.AddSeconds(30); });
        }
    }

    private KabanPolicy Policy(Guid actor)
    {
        var jobs = KabanJobCatalog.Jobs(ProtoMan, _ticker.CurrentPreset?.ID ?? _ticker.Preset?.ID);
        return new(actor, IsManager(actor), IsSenior(actor), _cfg.GetCVar(KabanCVars.Shop),
            _cfg.GetCVar(KabanCVars.Priorities) && !string.Equals(_ticker.CurrentPreset?.ID ?? _ticker.Preset?.ID, "ForceOnForce", StringComparison.OrdinalIgnoreCase),
            _cfg.GetCVar(KabanCVars.Tts), _cfg.GetCVar(KabanCVars.Discord),
            _cfg.GetCVar(KabanCVars.ServerId), _lobby.ToString(),
            !_lottery && _ticker.CanUpdateMap(), jobs.Where(IsCommand).Select(j => j.ID).ToArray(), jobs.Select(j => j.ID).ToArray(),
            Forcing: ForcingEnabled, ForceOptions: ForcingEnabled ? EntityManager.System<Content.Server.CMU14.Round.AuRoundSystem>().KabanCatalog() : []);
    }

    public static bool IsCommand(JobPrototype job)
        => job.MarineAuthorityLevel > 0 || job.RoundRole is
            "PlatoonCommander" or "ExecutiveOfficer" or "AdjutantDress" or "BrigadierGeneral" or
            "Advisor" or "JuniorOfficer" or "EngineeringOfficer" or "IntelOfficer" or "LogisticsOfficer" or "CMO" or "ChiefMP";

    public void Open(ICommonSession session)
    {
        if (_open.Any(e => e.Player == session))
            return;
        var eui = new KabanEui(this);
        _open.Add(eui);
        _euis.OpenEui(eui, session);
        Submit(eui, new KabanRequest { Action = KabanAction.Refresh });
    }
    public void Remove(KabanEui eui) => _open.Remove(eui);

    public void Submit(KabanEui eui, KabanRequest request)
    {
        var actor = (Guid) eui.Player.UserId;
        if (!Enabled)
        {
            eui.Result("cmu-kaban-disabled", request.Operation);
            return;
        }
        if (!_ready)
        {
            eui.Result("cmu-kaban-storage-unavailable", request.Operation);
            return;
        }
        if (_busy.Contains(actor) || _last.TryGetValue(actor, out var last) && DateTime.UtcNow < last.AddMilliseconds(500))
            return;
        _last[actor] = DateTime.UtcNow;
        var policy = Policy(actor);
        // A disarm accepted before the round starts must stop affecting the lottery immediately,
        // even if its database transaction completes during allocation.
        if (request.Action == KabanAction.Disarm && policy.Lobby)
            _armed.Remove(actor);
        if (request.Action == KabanAction.ApproveVoice && !policy.Manager
            || request.Action == KabanAction.GrantVoice && !policy.Senior)
        {
            eui.Result("cmu-kaban-forbidden", request.Operation);
            return;
        }
        _busy.Add(actor);
        _ = Run(eui, policy, request);
    }

    private async Task Run(KabanEui eui, KabanPolicy policy, KabanRequest request)
    {
        var result = "";
        try
        {
            // External availability is validated AFTER replay lookup inside the transaction.
            // A completed purchase must remain idempotent even if its model now exists or NTTS is offline.
            if (request.Action == KabanAction.GrantVoice || request.Action == KabanAction.Purchase && request.Product == "voice")
                policy = policy with { VoiceAvailable = await EntityManager.System<Content.Server.Corvax.TTS.TTSSystem>().CanRequestKabanSpeaker(request.Category) };
            if (request.Action == KabanAction.ApproveVoice)
                policy = policy with { VoiceVerified = await EntityManager.System<Content.Server.Corvax.TTS.TTSSystem>().CheckKabanSpeaker(request.Text) };
            if (request.Action != KabanAction.Refresh)
                result = await _db.KabanExecute(policy, request, DateTime.UtcNow);
            var view = await _db.KabanView(policy);
            var reservations = await _db.KabanReservations(policy.Server);
            var voices = await _db.KabanVoices();
            _completed.Enqueue(() =>
            {
                _busy.Remove(policy.Actor);
                // A late database reply cannot arm a token after the lottery has begun or after a restart.
                foreach (var token in reservations.Where(t => t.UserId == policy.Actor))
                {
                    if (_lottery || !Enabled || policy.Round != _lobby.ToString() || _ticker.RunLevel != GameRunLevel.PreRoundLobby)
                        _settlements.Enqueue((token.Reservation, false, "late-activation"));
                    else
                        _armed[token.UserId] = new(token.Id, token.Reservation, token.Multiplier, token.Expires,
                            JsonSerializer.Deserialize<string[]>(token.Jobs) ?? []);
                }
                if (!reservations.Any(t => t.UserId == policy.Actor))
                    _armed.Remove(policy.Actor);
                _voices.Clear();
                foreach (var voice in voices)
                    _voices[voice.Speaker] = voice;
                var currentPolicy = Policy(policy.Actor);
                view.PrioritiesEnabled = currentPolicy.Priorities;
                view.Lobby = currentPolicy.Lobby;
                view.ForcingEnabled = currentPolicy.Forcing;
                view.ForceOptions = currentPolicy.ForceOptions?.ToList() ?? [];
                foreach (var job in KabanJobCatalog.Jobs(ProtoMan, _ticker.CurrentPreset?.ID ?? _ticker.Preset?.ID))
                    view.Jobs[job.ID] = KabanJobCatalog.DisplayName(job);
                foreach (var session in _players.Sessions)
                    view.Accounts[session.UserId] = session.Name;
                view.Result = result;
                view.Response = request.Operation;
                eui.SetView(view);
            });
        }
        catch (KabanDenied denied)
        {
            _completed.Enqueue(() => { _busy.Remove(policy.Actor); eui.Result(denied.Message, request.Operation); });
        }
        catch (Exception e)
        {
            _log.Error($"Kaban request {request.Operation} failed; retry the SAME operation ID: {e}");
            _completed.Enqueue(() => { _busy.Remove(policy.Actor); eui.Result("cmu-kaban-storage-unavailable", request.Operation); });
        }
    }

    public bool CanUseVoice(Guid? owner, string speaker)
    {
        if (!_ready)
            return false;
        _voices.TryGetValue(speaker, out var voice);
        return KabanVoiceAccess.CanUse(voice, owner, Enabled, _cfg.GetCVar(KabanCVars.Tts));
    }
    public bool CanUploadVoice(Guid actor, string speaker)
        => Enabled && _ready && _cfg.GetCVar(KabanCVars.Tts) && IsManager(actor)
            && _voices.TryGetValue(speaker, out var voice) && voice.Status == "pending";

    public async Task<bool> ImportLegacyVoices(string[] speakers)
    {
        try
        {
            await _db.KabanImportLegacyVoices(speakers);
            await RefreshVoices();
            return true;
        }
        catch (Exception e)
        {
            _log.Warning($"Kaban legacy voice registration unavailable; custom access deferred: {e.Message}");
            return false;
        }
    }

    private async Task RefreshVoices()
    {
        try
        {
            var voices = await _db.KabanVoices();
            _completed.Enqueue(() =>
            {
                _voices.Clear();
                foreach (var voice in voices)
                    _voices[voice.Speaker] = voice;
                _refreshingVoices = false;
            });
        }
        catch (Exception e)
        {
            _log.Warning($"Kaban voice permissions refresh failed; last known registry retained: {e.Message}");
            _completed.Enqueue(() => _refreshingVoices = false);
        }
    }

    public void BeginLottery() { _lottery = true; _participated.Clear(); }
    public void AbortLottery() => ReturnReservations(false);

    public bool HasPriority(IEnumerable<Robust.Shared.Network.NetUserId> candidates, string job)
        => Enabled && _ready && _cfg.GetCVar(KabanCVars.Priorities) && candidates.Any(p => Weight(p, job) > 1);

    private int Weight(Guid player, string job)
        => _armed.TryGetValue(player, out var token) && token.Expires > DateTime.UtcNow.Ticks && token.Jobs.Contains(job)
            && (!_participated.TryGetValue(token.Reservation, out var chosen) || chosen == job) ? token.Weight : 1;

    public Robust.Shared.Network.NetUserId Pick(IReadOnlyCollection<Robust.Shared.Network.NetUserId> candidates,
        string job, int seats, IRobustRandom random)
    {
        if (candidates.Count <= seats || !HasPriority(candidates, job))
            return random.Pick(candidates);
        var players = candidates.ToArray();
        var weights = new int[players.Length];
        var total = 0;
        for (var i = 0; i < players.Length; i++)
        {
            weights[i] = Weight(players[i], job);
            total += weights[i];
            if (weights[i] > 1)
                _participated.TryAdd(_armed[players[i]].Reservation, job);
        }
        return players[KabanLottery.Select(weights, random.Next(total))];
    }

    private void OnRunLevel(GameRunLevelChangedEvent ev)
    {
        ForcingRunLevel(ev);
        if (ev.New == GameRunLevel.InRound)
            ReturnReservations(true);
    }

    private void ReturnReservations(bool success)
    {
        foreach (var token in _armed.Values)
        {
            var used = success && _participated.TryGetValue(token.Reservation, out _);
            _settlements.Enqueue((token.Reservation, used, _participated.GetValueOrDefault(token.Reservation, "")));
        }
        _armed.Clear();
        _participated.Clear();
    }

    private async Task Settle((Guid Id, bool Used, string Job) item)
    {
        try
        {
            var command = ProtoMan.TryIndex<JobPrototype>(item.Job, out var job) && IsCommand(job);
            await _db.KabanSettle(_cfg.GetCVar(KabanCVars.ServerId), item.Id, item.Used, item.Job, command, DateTime.UtcNow.Ticks);
            _completed.Enqueue(() => { _settlements.Dequeue(); _settling = false; });
        }
        catch (Exception e)
        {
            _log.Error($"Kaban token settlement failed; reservation retained for retry: {e}");
            _completed.Enqueue(() => { _settling = false; _retry = DateTime.UtcNow.AddSeconds(30); });
        }
    }

    private async Task DeliverDiscord(ulong guild, ulong boundary)
    {
        try
        {
            var lease = Guid.NewGuid().ToString();
            if (await _db.KabanClaimDiscord(lease, DateTime.UtcNow.Ticks) is { } role)
                await _discord.DeliverKabanRole(role, guild, boundary, _db, lease);
        }
        catch (Exception e)
        {
            _log.Warning($"Kaban Discord delivery deferred; purchase retained in outbox: {e.Message}");
        }
        finally
        {
            _completed.Enqueue(() => _delivering = false);
        }
    }
}

public sealed class KabanEui(KabanSystem system) : BaseEui
{
    private KabanState _view = new();
    public override EuiStateBase GetNewState() => _view;
    public override void Closed() => system.Remove(this);
    public void SetView(KabanState view)
    {
        if (IsShutDown)
            return;
        _view = view;
        StateDirty();
    }
    public void Result(string key, Guid response)
    {
        _view.Result = key;
        _view.Response = response;
        if (!IsShutDown)
            StateDirty();
    }
    public override void HandleMessage(Content.Shared.Eui.EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is KabanRequest request && !IsShutDown)
            system.Submit(this, request);
    }
}

[AnyCommand]
public sealed partial class KabanCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    public override string Command => "kabancoins";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player != null)
            _systems.GetEntitySystem<KabanSystem>().Open(shell.Player);
    }
}
