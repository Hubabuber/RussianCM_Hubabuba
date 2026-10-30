using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server._RMC14.LinkAccount;
using Content.Shared.Administration;
using Content.Shared.Corvax.TTS;
using Content.Shared.Corvax.CCCVars;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Enums;

namespace Content.Server.Corvax.TTS;

// CMU14 class: TTS voice selection, ordered delivery and playback.
public sealed partial class TTSSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private static readonly TimeSpan ReferenceVoiceCooldown = TimeSpan.FromSeconds(30);
    private readonly Dictionary<NetUserId, TimeSpan> _referenceVoiceCooldowns = new();
    private readonly HashSet<NetUserId> _referenceVoiceUploads = new();
    private readonly HashSet<string> _customVoices = new(StringComparer.Ordinal);
    private readonly HashSet<string> _referenceVoiceOperations = new(StringComparer.Ordinal);
    private bool _referenceVoiceDonorOnly;
    private bool _catalogLoaded;
    private Task<bool>? _catalogLoadTask;
    private TimeSpan _nextCatalogLoadAttempt;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ICommonSession, PreviewLimit> _catalogLimits = new();

    private void InitializeReferenceVoices()
    {
        SubscribeNetworkEvent<AddReferenceVoiceRequest>(OnAddReferenceVoice);
        SubscribeNetworkEvent<ReferenceVoiceCatalogRequest>(OnReferenceVoiceCatalogRequest);
        SubscribeNetworkEvent<DeleteReferenceVoiceRequest>(OnDeleteReferenceVoice);
        _cfg.OnValueChanged(CCCVars.TTSReferenceVoiceDonorOnly, OnReferenceVoiceDonorOnlyChanged, true);
        _linkAccount.PatronUpdated += OnPatronUpdated;
    }

    private void ShutdownReferenceVoices()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSReferenceVoiceDonorOnly, OnReferenceVoiceDonorOnlyChanged);
        _linkAccount.PatronUpdated -= OnPatronUpdated;
    }
    private async void OnAddReferenceVoice(AddReferenceVoiceRequest ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (!_isEnabled)
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.Disabled);
            return;
        }

        if (!CanCreateReferenceVoice(session))
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.NotDonor);
            return;
        }

        if (string.IsNullOrWhiteSpace(ev.SpeakerName) || !CustomTTSVoice.IsValidSpeakerName(ev.SpeakerName))
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.InvalidName);
            return;
        }

        // CMU14: only leadership may upload the consented model of a pending personal-voice application.
        if (!_kaban.CanUploadVoice(session.UserId, ev.SpeakerName))
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.NotDonor);
            return;
        }

        var userId = session.UserId;
        if (_referenceVoiceUploads.Contains(userId) ||
            _referenceVoiceCooldowns.TryGetValue(userId, out var cooldown) && cooldown > _timing.CurTime)
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.RateLimited);
            return;
        }

        if (ev.Audio == null || ev.Audio.Length > CustomTTSVoice.MaxAudioBytes)
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.FileTooLarge);
            return;
        }

        if (!CustomTTSVoice.IsValidWaveFile(ev.Audio))
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.InvalidAudio);
            return;
        }

        // Reserve the user and name before the first await to prevent overlapping uploads.
        if (!_referenceVoiceOperations.Add(ev.SpeakerName))
        {
            SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.AlreadyExists);
            return;
        }
        _referenceVoiceUploads.Add(userId);
        _referenceVoiceCooldowns[userId] = _timing.CurTime + ReferenceVoiceCooldown;
        try
        {
            if (!await EnsureReferenceVoiceCatalogLoaded() || !_isEnabled || !CanCreateReferenceVoice(session))
            {
                SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.ApiError);
                return;
            }
            if (_customVoices.Contains(ev.SpeakerName) ||
                _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>().Any(v => v.Speaker == ev.SpeakerName))
            {
                SendReferenceVoiceResult(session, ev.SpeakerName, AddReferenceVoiceResult.AlreadyExists);
                return;
            }
            var success = await _ttsManager.AddSpeaker(ev.SpeakerName, ev.Audio);
            if (success)
            {
                _customVoices.Add(ev.SpeakerName);
                BroadcastReferenceVoiceCatalog();
            }

            SendReferenceVoiceResult(session,
                ev.SpeakerName,
                success ? AddReferenceVoiceResult.Success : AddReferenceVoiceResult.ApiError);
        }
        finally
        {
            _referenceVoiceUploads.Remove(userId);
            _referenceVoiceOperations.Remove(ev.SpeakerName);
        }
    }

    private async void OnReferenceVoiceCatalogRequest(ReferenceVoiceCatalogRequest ev, EntitySessionEventArgs args)
    {
        var limit = _catalogLimits.GetValue(args.SenderSession, _ => new PreviewLimit());
        if (limit.ResetAt > _timing.CurTime)
            return;
        limit.ResetAt = _timing.CurTime + TimeSpan.FromSeconds(2);
        await EnsureReferenceVoiceCatalogLoaded();
        SendReferenceVoiceCatalog(args.SenderSession);
        SendReferenceVoiceAccess(args.SenderSession);
    }

    private void OnPatronUpdated((NetUserId Id, Content.Shared._RMC14.LinkAccount.SharedRMCPatronFull Patron) update)
    {
        if (_playerManager.TryGetSessionById(update.Id, out var session))
            SendReferenceVoiceAccess(session);
    }

    private void SendReferenceVoiceAccess(ICommonSession session)
    {
        if (session.Status != SessionStatus.InGame)
            return;
        RaiseNetworkEvent(new ReferenceVoiceAccessResponse(CanCreateReferenceVoice(session)), session);
    }

    private bool CanCreateReferenceVoice(ICommonSession session)
    {
        // CMU14: new uploads require a reviewed application; disabling the economy never grants free uploads.
        return _isEnabled && _kaban.Enabled && _kaban.IsManager(session.UserId);
        // return _isEnabled && (!_referenceVoiceDonorOnly || _linkAccount.GetConnectedPatron(session)?.Tier != null);
    }

    private void OnReferenceVoiceDonorOnlyChanged(bool donorOnly)
    {
        _referenceVoiceDonorOnly = donorOnly;
        foreach (var session in _playerManager.SessionsDict.Values)
            SendReferenceVoiceAccess(session);
    }

    private async void OnDeleteReferenceVoice(DeleteReferenceVoiceRequest ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Host))
        {
            SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.Forbidden);
            return;
        }

        if (string.IsNullOrWhiteSpace(ev.SpeakerName) || !CustomTTSVoice.IsValidSpeakerName(ev.SpeakerName))
        {
            SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.InvalidName);
            return;
        }

        if (!await EnsureReferenceVoiceCatalogLoaded())
        {
            SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.ApiError);
            return;
        }

        // Only names returned by NTTS as custom voices can be deleted. This protects built-in speakers.
        if (!_customVoices.Contains(ev.SpeakerName))
        {
            SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.NotFound);
            return;
        }

        if (!_referenceVoiceOperations.Add(ev.SpeakerName))
        {
            SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.ApiError);
            return;
        }

        try
        {
            if (!await _ttsManager.DeleteSpeaker(ev.SpeakerName))
            {
                SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.ApiError);
                return;
            }

            _customVoices.Remove(ev.SpeakerName);
            _ttsManager.ResetCache();
            BroadcastReferenceVoiceCatalog();
            SendDeleteReferenceVoiceResult(session, ev.SpeakerName, DeleteReferenceVoiceResult.Success);
        }
        finally
        {
            _referenceVoiceOperations.Remove(ev.SpeakerName);
        }
    }

    private Task<bool> EnsureReferenceVoiceCatalogLoaded()
    {
        if (!_isEnabled)
            return Task.FromResult(false);

        if (_catalogLoaded)
            return Task.FromResult(true);

        if (_catalogLoadTask != null)
            return _catalogLoadTask;

        if (_timing.CurTime < _nextCatalogLoadAttempt)
            return Task.FromResult(false);

        _catalogLoadTask = LoadReferenceVoiceCatalog();
        return _catalogLoadTask;
    }

    private async Task<bool> LoadReferenceVoiceCatalog()
    {
        // Ensure EnsureReferenceVoiceCatalogLoaded assigns the coalesced task before this method can clear it.
        await Task.Yield();
        try
        {
            var voices = await _ttsManager.GetCustomSpeakers();
            if (voices == null)
            {
                _nextCatalogLoadAttempt = _timing.CurTime + TimeSpan.FromSeconds(30);
                return false;
            }

            // CMU14: freeze legacy access once, before new models can be sold. A storage outage cannot throw into round startup.
            if (!await _kaban.ImportLegacyVoices(voices.ToArray()))
            {
                _nextCatalogLoadAttempt = _timing.CurTime + TimeSpan.FromSeconds(30);
                return false;
            }

            _customVoices.Clear();
            _customVoices.UnionWith(voices);
            _catalogLoaded = true;
            BroadcastReferenceVoiceCatalog();
            return true;
        }
        finally
        {
            _catalogLoadTask = null;
        }
    }

    private void SendReferenceVoiceCatalog(ICommonSession session)
    {
        if (session.Status != SessionStatus.InGame)
            return;
        RaiseNetworkEvent(new ReferenceVoiceCatalogResponse(GetReferenceVoiceCatalog(session)), session); // CMU14
    }

    private void BroadcastReferenceVoiceCatalog()
    {
        // RaiseNetworkEvent(new ReferenceVoiceCatalogResponse(GetReferenceVoiceCatalog())); // CMU14: per-account access.
        foreach (var session in _playerManager.Sessions) // CMU14
            SendReferenceVoiceCatalog(session);
    }

    private string[] GetReferenceVoiceCatalog(ICommonSession session) // CMU14
    {
        return _customVoices.Where(s => _kaban.CanUseVoice(session.UserId, s)).Order(StringComparer.OrdinalIgnoreCase).ToArray(); // CMU14
    }

    private void SendDeleteReferenceVoiceResult(
        ICommonSession session,
        string speakerName,
        DeleteReferenceVoiceResult result)
    {
        if (session.Status != SessionStatus.InGame)
            return;
        RaiseNetworkEvent(new DeleteReferenceVoiceResponse(speakerName, result), session);
    }

    private void SendReferenceVoiceResult(
        ICommonSession session,
        string speakerName,
        AddReferenceVoiceResult result)
    {
        if (session.Status != SessionStatus.InGame)
            return;
        RaiseNetworkEvent(new AddReferenceVoiceResponse(speakerName, result), session);
    }

    private bool TryResolveSpeaker(string voiceId, out string speaker, Content.Shared.Humanoid.Sex sex = Content.Shared.Humanoid.Sex.Unsexed, Guid? owner = null) // CMU14
    {
        if (_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var prototype))
        {
            speaker = prototype.Speaker;
            return true;
        }

        if (CustomTTSVoice.TryGetSpeaker(voiceId, out speaker) && _customVoices.Contains(speaker) && _kaban.CanUseVoice(owner, speaker)) // CMU14
            return true;
        // A deleted custom voice must not leave an existing character permanently silent.
        if (CustomTTSVoice.TryGetSpeaker(voiceId, out _) && _catalogLoaded)
        {
            var fallbacks = CMUTTSVoiceSelection.GetRandomVoices(_prototypeManager.EnumeratePrototypes<TTSVoicePrototype>(), sex);
            var fallback = fallbacks.FirstOrDefault(voice => voice.ID == Content.Shared.Preferences.HumanoidCharacterProfile.DefaultTTSVoice)
                ?? fallbacks.FirstOrDefault();
            if (fallback != null)
            {
                speaker = fallback.Speaker;
                return true;
            }
        }
        return false;
    }

}
