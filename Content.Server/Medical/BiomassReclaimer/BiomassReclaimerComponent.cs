using Content.Shared.Storage;

namespace Content.Server.Medical.BiomassReclaimer
{
    [RegisterComponent]
    public sealed partial class BiomassReclaimerComponent : Component
    {
        /// <summary>
        /// This gets set for each mob it processes.
        /// When it hits 0, there is a chance for the reclaimer to either spill blood or throw an item.
        /// </summary>
        [ViewVariables]
        public float RandomMessTimer = 0f;

        /// <summary>
        /// The interval for <see cref="RandomMessTimer"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("randomMessInterval")]
        public TimeSpan RandomMessInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// This gets set for each mob it processes.
        /// When it hits 0, spit out biomass.
        /// </summary>
        [ViewVariables]
        public float ProcessingTimer = default;

        /// <summary>
        /// Amount of biomass that the mob being processed will yield.
        /// This is calculated from the YieldPerUnitMass.
        /// </summary>
        [ViewVariables]
        public int CurrentExpectedYield = default;

        /// <summary>
        /// The reagent that will be spilled while processing a mob.
        /// </summary>
        [ViewVariables]
        public string? BloodReagent;

        /// <summary>
        /// Entities that can be randomly spawned while processing a mob.
        /// </summary>
        public List<EntitySpawnEntry> SpawnedEntities = new();

        /// <summary>
        /// How many units of biomass it produces for each unit of mass.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float YieldPerUnitMass = 0.4f;

        /// <summary>
        /// The time it takes to process a mob, per mass.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float ProcessingTimePerUnitMass = 0.5f;

        /// <summary>
        /// Will this refuse to gib a living mob?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("safetyEnabled")]
        public bool SafetyEnabled = true;
    }
}
