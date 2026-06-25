using System.Linq;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RuMC14.Vendors;

namespace Content.Server._RuMC14.Vendors;

public sealed class RuMC14VendorGameModeFilterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RuMC14VendorGameModeFilterComponent, MapInitEvent>(OnMapInit,
            before: [typeof(SharedCMAutomatedVendorSystem)]);
    }

    private void OnMapInit(Entity<RuMC14VendorGameModeFilterComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<CMAutomatedVendorComponent>(ent, out var vendor))
            return;

        var preset = EntityManager.System<GameTicker>().Preset;
        if (preset == null)
            return;

        foreach (var section in vendor.Sections)
        {
            section.Entries.RemoveAll(e =>
                ent.Comp.BlockedEntries.TryGetValue(e.Id, out var blockedModes) &&
                blockedModes.Any(m => m.Equals(preset.ID, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
