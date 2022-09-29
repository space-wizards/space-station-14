using Content.Shared.Storage;
using System.Threading;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Medical.BiomassReclaimer
{
    [RegisterComponent]
    public sealed class BiomassReclaimerComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("randomMessAccumulator")]
        public float RandomMessAccumulator = 0f;
        public TimeSpan RandomMessInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// This gets set for each mob it processes.
        /// When accumulator hits this, spit out biomass.
        /// </summary>
        public float CurrentProcessingTime = 70f;

        /// <summary>
        /// This is calculated from the YieldPerUnitMass
        /// and adjusted for genetic damage too.
        /// </summary>
        public float CurrentExpectedYield = 28f;

        public string BloodReagent = "Blood";

        public List<EntitySpawnEntry> SpawnedEntities = new();

        /// <summary>
        /// How many units of biomass it produces for each unit of mass.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float YieldPerUnitMass = 0.4f;

        /// <summary>
        /// The base yield when no components are upgraded
        /// </summary>
        [DataField("baseYieldPerUnitMass")]
        public float BaseYieldPerUnitMass = 0.4f;

        /// <summary>
        /// Machine part whose rating modifies the yield per mass.
        /// </summary>
        [DataField("machinePartYieldAmount", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartYieldAmount = "Manipulator";

        /// <summary>
        /// Lower number = faster processing.
        /// Good for machine upgrading I guess.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ProcessingSpeedMultiplier = 0.4f;

        /// <summary>
        /// The base multiplier of processing speed with no upgrades
        /// that is used with the weight to calculate the yield
        /// </summary>
        [DataField("baseProcessingSpeedMultiplier")]
        public float BaseProcessingSpeedMultiplier = 0.4f;

        /// <summary>
        /// The machine part that increses the processing speed.
        /// </summary>
        [DataField("machinePartProcessSpeed", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartProcessingSpeed = "Laser";

        /// <summary>
        /// Will this refuse to gib a living mob?
        /// </summary>
        [DataField("safetyEnabled")]
        public bool SafetyEnabled = true;
    }
}
