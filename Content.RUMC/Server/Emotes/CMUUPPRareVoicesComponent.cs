namespace Content.Server.CMU14.Emotes;

/// <summary>
/// Rare vocal emote variants granted by the character's UPP origin at spawn.
/// </summary>
[RegisterComponent]
public sealed partial class CMUUPPRareVoicesComponent : Component
{
    [DataField]
    public float Chance = 0.1f;
}
