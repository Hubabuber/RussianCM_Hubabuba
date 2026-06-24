namespace Content.Server._CMU14.Acquaintance;

/// <summary>
/// Stores names remembered by a character. This component lives on the mind entity,
/// so the memories follow the character rather than the body.
/// </summary>
[RegisterComponent]
public sealed partial class AcquaintanceComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<EntityUid, string> KnownFaces = new();

    [ViewVariables]
    public readonly Dictionary<string, string> KnownVoices = new(StringComparer.Ordinal);
}
