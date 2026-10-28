using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._RuCM.Discord;

[Serializable, NetSerializable]
public sealed class DiscordLinkRequirementEvent : EntityEventArgs
{
    public bool Required;
}
