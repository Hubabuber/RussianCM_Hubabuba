using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.CMU14.Round;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._RuCM.Qualifications;

public sealed partial class QualificationSystem
{
    [Dependency] private AuRoundSystem _round = default!;
    [Dependency] private QualificationRolePolicy _policy = default!;
    private float _policyTimer;
    private long _policyRevision = -1;
    private QualificationService? _policySource;
    private HashSet<string> _policyJobs = new();
    [Dependency] private Content.Shared.Interaction.SharedInteractionSystem _interaction = default!;
    private readonly Dictionary<Guid, string> _sentPolicy = new();
    private readonly Dictionary<Guid, DateTimeOffset> _entryRate = new();
    private readonly Dictionary<Guid, DateTimeOffset> _policyRequests = new();
    public bool IsInsurgency => string.Equals(_ticker.RunLevel == Content.Server.GameTicking.GameRunLevel.PreRoundLobby
        ? _round.SelectedPreset?.ID ?? _ticker.Preset?.ID
        : _ticker.CurrentPreset?.ID ?? _ticker.Preset?.ID ?? _round.SelectedPreset?.ID, "Insurgency", StringComparison.OrdinalIgnoreCase);
    private static bool IsDrillInstructor(QualificationAuthority actor) => actor.CurrentParticipant && QualificationRules.IsDrillInstructor(actor.Context.Job);
    // CMU14 method: policy polling must not copy the qualification store for each player.
    public bool CanBrowseRecords(ICommonSession player)
    {
        var actor = Authority(player);
        return Service.IsManagement(actor) || actor.CurrentOfficer || actor.CurrentCo || IsDrillInstructor(actor) ||
            Service.IsActiveInstructor(player.UserId);
    }
    private HashSet<string> PolicyJobs()
    {
        if (_policySource == Service && _policyRevision == Service.Revision) return _policyJobs;
        _policySource = Service;
        _policyRevision = Service.Revision;
        _policyJobs = Service.EnabledJobIds().Concat(new[] { "AU14JobGOVFORadvisor", "AU14JobGOVFORadvisorRMC", "AU14JobGOVFORadvisorUPP" }).Where(id =>
            ProtoMan.TryIndex<Content.Shared.Roles.JobPrototype>(id, out var job) &&
            job.RoundSide == Content.Shared.CMU14.Round.Roles.RoundJobSide.Govfor && !job.IsSynthetic).ToHashSet();
        return _policyJobs;
    }
    public void SynchronizeRolePolicy() => _policy.Apply(Mode == QualificationMode.Enforce, PolicyJobs());
    private void InitializeEntryPoints()
    {
        SubscribeNetworkEvent<QualificationEntryRequest>((_, args) =>
        {
            var now = DateTimeOffset.UtcNow;
            if (_entryRate.TryGetValue(args.SenderSession.UserId, out var at) && now - at < TimeSpan.FromSeconds(1)) return;
            _entryRate[args.SenderSession.UserId] = now;
            Open(args.SenderSession);
        });
        SubscribeNetworkEvent<QualificationPolicyRequest>((_, args) =>
        {
            var now = DateTimeOffset.UtcNow;
            if (_policyRequests.TryGetValue(args.SenderSession.UserId, out var at) && now - at < TimeSpan.FromSeconds(1)) return;
            _policyRequests[args.SenderSession.UserId] = now;
            SendPolicy(args.SenderSession, true);
        });
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetRecordVerb);
    }
    private void UpdateEntryPoints(float frameTime)
    {
        if (!_ready) return;
        SynchronizeRolePolicy();
        _policyTimer -= frameTime;
        if (_policyTimer > 0) return;
        _policyTimer = 1;
        foreach (var player in _players.Sessions.Where(Online)) SendPolicy(player);
        foreach (var id in _sentPolicy.Keys.Where(id => !TargetOnline(id)).ToArray())
        { _sentPolicy.Remove(id); _entryRate.Remove(id); _policyRequests.Remove(id); }
    }
    private void SendPolicy(ICommonSession player, bool force = false)
    {
        if (!_ready || !Online(player)) return;
        var active = Mode == QualificationMode.Enforce;
        var state = new QualificationPolicyState { Active = active, Staff = CanBrowseRecords(player) || _admins.IsAdmin(player),
            Jobs = PolicyJobs(), Baseline = _policy.Apply(active, PolicyJobs()) };
        foreach (var job in state.Jobs)
        {
            state.Eligibility[job] = CanTakeJob(player.UserId, job);
            if (_policy.BaseRequirements.TryGetValue(job, out var requirements)) state.BaseRequirements[job] = new(requirements);
        }
        var signature = state.Active + ":" + state.Staff + ":" + state.Baseline + ":" + string.Join(",", state.Eligibility.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value));
        if (!force && _sentPolicy.GetValueOrDefault(player.UserId) == signature) return;
        _sentPolicy[player.UserId] = signature;
        RaiseNetworkEvent(state, player);
    }
    private void GetRecordVerb(GetVerbsEvent<Verb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract || !TryComp<ActorComponent>(ev.User, out var actor) ||
            !TryComp<ActorComponent>(ev.Target, out var target) || actor.PlayerSession == target.PlayerSession ||
            !CanBrowseRecords(actor.PlayerSession)) return;
        var userEntity = ev.User; var targetEntity = ev.Target;
        ev.Verbs.Add(new Verb { Text = Loc.GetString("rucm-qualifications-open-record-verb"), Priority = 10,
            Act = () => OpenCharacterRecord(actor.PlayerSession, userEntity, targetEntity) });
    }
    public void OpenCharacterRecord(ICommonSession player, EntityUid user, EntityUid target)
    {
        // Re-resolve identity and access after the menu was opened; no client-supplied account ID.
        if (player.AttachedEntity != user || !Online(player) || !CanBrowseRecords(player) ||
            !TryComp<ActorComponent>(target, out var actor) || actor.PlayerSession.AttachedEntity != target ||
            !Online(actor.PlayerSession) || !_interaction.InRangeUnobstructed(user, Transform(target).Coordinates)) return;
        Open(player, target: actor.PlayerSession.UserId);
    }
}
