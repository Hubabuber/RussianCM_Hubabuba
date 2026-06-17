using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class HardpointSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private readonly record struct VehicleHardpointFailureRepairStep(
        ProtoId<ToolQualityPrototype> Tool,
        float Time,
        string Instruction,
        bool RequiresWelder = false);

    private static readonly VehicleHardpointFailureRepairStep[] ArmorCompromisedRepairSteps = // RuMC edit
    {
        new("Anchoring", 4f, "rmc-hardpoint-repair-armor-compromised-1"),
        new("Welding", 8f, "rmc-hardpoint-repair-armor-compromised-2", true),
    };

    private static readonly VehicleHardpointFailureRepairStep[] FeedJamRepairSteps = // RuMC edit
    {
        new("Screwing", 4f, "rmc-hardpoint-repair-feed-jam-1"),
        new("Pulsing", 5f, "rmc-hardpoint-repair-feed-jam-2"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] RunawayTriggerRepairSteps = // RuMC edit
    {
        new("Screwing", 5f, "rmc-hardpoint-repair-runaway-trigger-1"),
        new("Pulsing", 6f, "rmc-hardpoint-repair-runaway-trigger-2"),
        new("Anchoring", 5f, "rmc-hardpoint-repair-runaway-trigger-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] TurretTraverseRepairSteps = // RuMC edit
    {
        new("Anchoring", 6f, "rmc-hardpoint-repair-turret-traverse-1"),
        new("VehicleServicing", 5f, "rmc-hardpoint-repair-turret-traverse-2"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] EngineMisfireRepairSteps = // RuMC edit
    {
        new("Screwing", 4f, "rmc-hardpoint-repair-engine-misfire-1"),
        new("Pulsing", 6f, "rmc-hardpoint-repair-engine-misfire-2"),
        new("Anchoring", 4f, "rmc-hardpoint-repair-engine-misfire-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] TransmissionSlipRepairSteps = // RuMC edit
    {
        new("VehicleServicing", 7f, "rmc-hardpoint-repair-transmission-slip-1"),
        new("Anchoring", 5f, "rmc-hardpoint-repair-transmission-slip-2"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] WarpedFrameRepairSteps = // RuMC edit
    {
        new("VehicleServicing", 8f, "rmc-hardpoint-repair-warped-frame-1"),
        new("Welding", 12f, "rmc-hardpoint-repair-warped-frame-2", true),
        new("Anchoring", 6f, "rmc-hardpoint-repair-warped-frame-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] DamagedMountRepairSteps = // RuMC edit
    {
        new("VehicleServicing", 6f, "rmc-hardpoint-repair-damaged-mount-1"),
        new("Anchoring", 6f, "rmc-hardpoint-repair-damaged-mount-2"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] TireBlowoutRepairSteps = // RuMC edit
    {
        new("Prying", 5f, "rmc-hardpoint-repair-tire-blowout-1"),
        new("VehicleServicing", 6f, "rmc-hardpoint-repair-tire-blowout-2"),
        new("Anchoring", 5f, "rmc-hardpoint-repair-tire-blowout-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] ThrownTreadRepairSteps = // RuMC edit
    {
        new("VehicleServicing", 8f, "rmc-hardpoint-repair-thrown-tread-1"),
        new("Prying", 6f, "rmc-hardpoint-repair-thrown-tread-2"),
        new("Anchoring", 8f, "rmc-hardpoint-repair-thrown-tread-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] EngineOverheatRepairSteps = // RuMC edit
    {
        new("Screwing", 4f, "rmc-hardpoint-repair-engine-overheat-1"),
        new("Prying", 5f, "rmc-hardpoint-repair-engine-overheat-2"),
        new("Pulsing", 6f, "rmc-hardpoint-repair-engine-overheat-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] ElectricalShortRepairSteps = // RuMC edit
    {
        new("Cutting", 5f, "rmc-hardpoint-repair-electrical-short-1"),
        new("Pulsing", 6f, "rmc-hardpoint-repair-electrical-short-2"),
        new("Screwing", 4f, "rmc-hardpoint-repair-electrical-short-3"),
    };

    private static readonly VehicleHardpointFailureRepairStep[] FuelLeakRepairSteps = // RuMC edit
    {
        new("Screwing", 4f, "rmc-hardpoint-repair-fuel-leak-1"),
        new("Welding", 7f, "rmc-hardpoint-repair-fuel-leak-2", true),
        new("Anchoring", 4f, "rmc-hardpoint-repair-fuel-leak-3"),
    };
}
