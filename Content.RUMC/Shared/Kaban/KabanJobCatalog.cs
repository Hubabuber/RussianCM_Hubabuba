using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Roles;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._RuCM.Kaban;

/// <summary>The visible character-setup role catalogue, shared by the editor and the reward UI.</summary>
public static class KabanJobCatalog
{
    public static IEnumerable<string> Presets(DepartmentPrototype department, JobPrototype job)
    {
        if (department.EditorHidden || department.Hidden || !department.IsCM || !job.SetPreference || job.Hidden)
            return [];
        string[] presets;
        if (department.Faction == "govfor")
            presets = ["ForceOnForce", "Insurgency", "DistressSignal"];
        else if (department.Faction == "opfor")
            presets = [];
        else if (department.ID == "AU14DepartmentColonialLiberationFront")
            presets = ["Insurgency"];
        else if (department.Faction == "colonist")
            presets = ["Insurgency", "ColonyFall"];
        else if (job.ID is "AU14JobThreatLeader" or "AU14JobThreatMember" or "AU14JobThirdPartyLeader" or "AU14JobThirdPartyMember")
            presets = ["ColonyFall", "DistressSignal"];
        else
            presets = [];
        return presets.Where(job.IsAvailableInCharacterSetup);
    }

    public static JobPrototype[] Jobs(IPrototypeManager prototypes, string? preset = null)
        => prototypes.EnumeratePrototypes<DepartmentPrototype>()
            .SelectMany(department => department.Roles.Select(id => prototypes.Index(id))
                .Where(job => Presets(department, job).Any(mode => string.IsNullOrEmpty(preset)
                    || string.Equals(mode, preset, StringComparison.OrdinalIgnoreCase))))
            .DistinctBy(job => job.ID).OrderBy(job => job.ID, StringComparer.Ordinal).ToArray();

    public static string DisplayName(JobPrototype job)
    {
        var key = string.IsNullOrWhiteSpace(job.SpawnMenuRoleName) ? job.Name : job.SpawnMenuRoleName;
        var name = (Loc.TryGetString(key, out var localized) ? localized : key).TrimEnd();
        foreach (var suffix in new[] { " (GOVFOR)", " (OPFOR)" })
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name[..^suffix.Length].TrimEnd();
        return name;
    }
}
