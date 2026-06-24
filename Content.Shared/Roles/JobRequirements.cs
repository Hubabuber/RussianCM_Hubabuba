using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RuMC14.RoleTests;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public static class JobRequirements
{
    public static bool TryRequirementsMet(
        JobPrototype job,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        bool roleTimersEnabled = true,
        bool roleTimerExcluded = false)
    {
        var sys = entManager.System<SharedRoleSystem>();
        var requirements = GetActiveRequirements(sys.GetJobRequirement(job), roleTimersEnabled, roleTimerExcluded);
        reason = null;
        if (requirements == null)
            return true;

        foreach (var requirement in requirements)
        {
            if (!requirement.Check(entManager, protoManager, profile, playTimes, out reason))
                return false;
        }

        return true;
    }

    public static HashSet<JobRequirement>? GetActiveRequirements(
        HashSet<JobRequirement>? requirements,
        bool roleTimersEnabled,
        bool roleTimerExcluded)
    {
        if (requirements == null)
            return null;

        if (roleTimersEnabled && !roleTimerExcluded)
            return requirements;

        var roleTestRequirements = requirements
            .Where(requirement => requirement is RoleTestRequirement)
            .ToHashSet();

        return roleTestRequirements.Count == 0 ? null : roleTestRequirements;
    }
}

/// <summary>
/// Abstract class for playtime and other requirements for role gates.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class JobRequirement
{
    [DataField]
    public bool Inverted;

    public abstract bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason);
}
