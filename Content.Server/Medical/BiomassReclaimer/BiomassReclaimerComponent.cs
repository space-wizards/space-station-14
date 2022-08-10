using Content.Shared.Storage;
using System.Threading;

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
        [DataField("yieldPerUnitMass")]
        public float YieldPerUnitMass = 0.4f;

        /// <summary>
        /// Lower number = faster processing.
        /// Good for machine upgrading I guess.
        /// </summmary>
        public float ProcessingSpeedMultiplier = 0.5f;


        /// <summary>
        /// Will this refuse to gib a living mob?
        /// </summary>
        [DataField("safetyEnabled")]
        public bool SafetyEnabled = true;
    }
}
