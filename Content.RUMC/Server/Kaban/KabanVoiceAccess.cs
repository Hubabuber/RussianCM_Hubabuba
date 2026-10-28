using Content.Server.Database;

namespace Content.Server._RuCM.Kaban;

// CMU14: unknown names fail closed on every node; only the once-imported legacy catalog is public.
public static class KabanVoiceAccess
{
    public static bool CanUse(KabanVoice? voice, Guid? owner, bool enabled, bool personalTts)
        => voice != null && (voice.Status == "legacy"
            || enabled && personalTts && voice.Status == "active" && voice.UserId == owner);
}
