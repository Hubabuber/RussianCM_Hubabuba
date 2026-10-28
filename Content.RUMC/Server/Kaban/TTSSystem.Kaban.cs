using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._RuCM.Kaban;
using Content.Shared.Corvax.TTS;

namespace Content.Server.Corvax.TTS;

// CMU14: new personal voices are account-scoped; pre-existing catalog entries retain their access.
public sealed partial class TTSSystem
{
    [Dependency] private readonly KabanSystem _kaban = default!;

    public bool HasKabanSpeaker(string name)
        => _customVoices.Contains(name) || _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>().Any(v => v.Speaker == name);

    public async Task<bool> CanRequestKabanSpeaker(string name)
        => _isEnabled && CustomTTSVoice.IsValidSpeakerName(name) && await EnsureReferenceVoiceCatalogLoaded() && !HasKabanSpeaker(name);

    public async Task<bool> CheckKabanSpeaker(string name)
    {
        if (!_isEnabled || !CustomTTSVoice.IsValidSpeakerName(name))
            return false;
        var catalog = await _ttsManager.GetCustomSpeakers();
        if (catalog == null || !catalog.Contains(name))
            return false;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var test = await _ttsManager.ConvertTextToSpeech(name, Loc.GetString("tts-preview-text"), timeout.Token);
        if (test is not { Length: > 0 })
            return false;
        _customVoices.UnionWith(catalog);
        return true;
    }

    private Guid? KabanOwner(EntityUid? source)
        => source is { } entity && _playerManager.TryGetSessionByEntity(entity, out var session) ? session.UserId : null;
}
