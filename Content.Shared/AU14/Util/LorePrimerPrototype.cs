using Robust.Shared.Prototypes;

namespace Content.Shared.AU14.util;
[Prototype]
public sealed partial class LorePrimerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

        [DataField("KnowledgeLevelsGovforThreat", required: false)]
        public Dictionary<string, int>? KnowledgeLevels { get; private set; }
        // randomly select a level
        [DataField("planetText", required: false)]
        public LocId? PlanetText { get; private set; } // RuMC edit


        [DataField("PlatoonInfo", required: false)]
        public LocId? PlatoonInfo { get; private set; } // RuMC edit



        [DataField("threattext", required: false)]
        public string? ThreatText { get; private set; }


}

