using System.Threading;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
        [ViewVariables(VVAccess.ReadWrite)]
        public float YieldPerUnitMass = default;

        /// <summary>
        /// The base yield per mass unit when no components are upgraded.
        /// </summary>
        [DataField("baseYieldPerUnitMass")]
        public float BaseYieldPerUnitMass = 0.4f;

        /// <summary>
        /// Machine part whose rating modifies the yield per mass.
        /// </summary>
        [DataField("machinePartYieldAmount", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartYieldAmount = "MatterBin";

        /// <summary>
        /// How much the machine part quality affects the yield.
        /// Going up a tier will multiply the yield by this amount.
        /// </summary>
        [DataField("partRatingYieldAmountMultiplier")]
        public float PartRatingYieldAmountMultiplier = 1.25f;

        /// <summary>
        /// The time it takes to process a mob, per mass.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ProcessingTimePerUnitMass = default;

        /// <summary>
        /// The base time per mass unit that it takes to process a mob
        /// when no components are upgraded.
        /// </summary>
        [DataField("baseProcessingTimePerUnitMass")]
        public float BaseProcessingTimePerUnitMass = 0.5f;

        /// <summary>
        /// The machine part that increses the processing speed.
        /// </summary>
        [DataField("machinePartProcessSpeed", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartProcessingSpeed = "Manipulator";

        /// <summary>
        /// How much the machine part quality affects the yield.
        /// Going up a tier will multiply the speed by this amount.
        /// </summary>
        [DataField("partRatingSpeedMultiplier")]
        public float PartRatingSpeedMultiplier = 1.35f;

        /// <summary>
        /// Will this refuse to gib a living mob?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("safetyEnabled")]
        public bool SafetyEnabled = true;
    }
}
