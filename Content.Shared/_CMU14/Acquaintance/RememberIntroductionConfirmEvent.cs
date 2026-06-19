using Robust.Shared.Serialization;

namespace Content.Shared._CMU14.Acquaintance;

[Serializable, NetSerializable]
public sealed record RememberIntroductionConfirmEvent(
    NetEntity Speaker,
    string ClaimedName,
    string VoiceName,
    bool FaceVisible);
