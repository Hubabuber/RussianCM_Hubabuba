// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Content.Server.Administration;
using Content.Shared._AU14.ZLevelBuilding;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._AU14.ZLevelBuilding;

/// <summary>
/// Building overhaul (z-level): lists every map with its AU14 "Multi Z-Level" status (whether players may build
/// the overhaul's stairs / vertical floors there) and lets an admin toggle it per map or globally at runtime.
///
/// This is the live counterpart to the mapper opt-out (<see cref="ZBuildableMapComponent"/> <c>enabled: false</c>
/// in a map file): use it to confirm which maps allow z-building and to switch a map off so players can't build
/// under it. The toggle is networked (the build condition + cave-in vignette respect it immediately) but, like
/// any runtime change, it is not persisted - bake it into the map prototype to make it permanent.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class AU14MultiZCommand : IConsoleCommand
{
    public string Command => "au_multiz";
    public string Description => Loc.GetString("cmd-au-multiz-desc");
    public string Help => Loc.GetString("cmd-au-multiz-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var building = entMan.System<ZLevelBuildingSystem>();

        // No args: list every map.
        if (args.Length == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-au-multiz-global-status",
                ("state", Loc.GetString(building.GloballyEnabled ? "cmd-au-multiz-enabled" : "cmd-au-multiz-disabled"))));
            var query = entMan.AllEntityQueryEnumerator<MapComponent>();
            while (query.MoveNext(out var uid, out var map))
            {
                var yes = building.IsEnabledOn(uid);
                shell.WriteLine(Loc.GetString("cmd-au-multiz-map-status",
                    ("id", map.MapId),
                    ("map", entMan.ToPrettyString(uid)),
                    ("state", Loc.GetString(yes ? "cmd-au-multiz-yes" : "cmd-au-multiz-no"))));
            }
            return;
        }

        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-au-multiz-usage"));
            return;
        }

        var on = args[1].Equals("on", StringComparison.OrdinalIgnoreCase);
        if (!on && !args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            shell.WriteError(Loc.GetString("cmd-au-multiz-invalid-state"));
            return;
        }

        // Global switch.
        if (args[0].Equals("global", StringComparison.OrdinalIgnoreCase))
        {
            building.GloballyEnabled = on;
            shell.WriteLine(Loc.GetString("cmd-au-multiz-global-changed",
                ("state", Loc.GetString(on ? "cmd-au-multiz-enabled" : "cmd-au-multiz-disabled"))));
            return;
        }

        if (!int.TryParse(args[0], out var mapIdInt))
        {
            shell.WriteError(Loc.GetString("cmd-au-multiz-invalid-map"));
            return;
        }

        var mapManager = IoCManager.Resolve<SharedMapSystem>();
        var mapId = new MapId(mapIdInt);
        if (!mapManager.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-au-multiz-map-not-found", ("id", mapIdInt)));
            return;
        }

        var mapUid = mapManager.GetMapOrInvalid(mapId);
        var comp = entMan.EnsureComponent<ZBuildableMapComponent>(mapUid);
        comp.Enabled = on;
        entMan.Dirty(mapUid, comp);

        shell.WriteLine(Loc.GetString("cmd-au-multiz-map-changed",
            ("id", mapIdInt),
            ("state", Loc.GetString(on ? "cmd-au-multiz-yes" : "cmd-au-multiz-no")),
            ("permission", Loc.GetString(on ? "cmd-au-multiz-can-build" : "cmd-au-multiz-cannot-build"))));
    }
}
