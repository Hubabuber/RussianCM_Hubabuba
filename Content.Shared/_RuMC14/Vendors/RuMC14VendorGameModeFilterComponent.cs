using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._RuMC14.Vendors;

/// <summary>
/// Removes specific vendor entries when the active game preset matches
/// any of the listed preset IDs.
/// </summary>
[RegisterComponent]
public sealed partial class RuMC14VendorGameModeFilterComponent : Component
{
    /// <summary>
    /// Key: EntProtoId of the vendor entry to block.
    /// Value: list of preset IDs that trigger the block.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, List<string>> BlockedEntries = new();
}
