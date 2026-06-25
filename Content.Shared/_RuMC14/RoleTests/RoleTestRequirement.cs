using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RuMC14.RoleTests;

[Serializable, NetSerializable]
public sealed partial class RoleTestRequirement : JobRequirement
{
    [DataField(required: true)]
    public string Test = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    public override bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var tracker = RoleTestShared.GetTracker(Test);
        if (playTimes.TryGetValue(tracker, out var passed) && passed > TimeSpan.Zero)
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            "role-test-requirement-missing",
            ("test", Name)));
        return false;
    }
}
