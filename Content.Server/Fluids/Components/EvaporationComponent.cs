using Content.Server.Fluids.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    [Access(typeof(EvaporationSystem))]
    public sealed class EvaporationComponent : Component
    {
        /// <summary>
        ///     Is this entity actively evaporating? This toggle lets us pause evaporation under certain conditions.
        /// </summary>
        [DataField("evaporationToggle")]
        public bool EvaporationToggle = true;

        /// <summary>
        ///     The time that it will take this puddle to lose one fixed unit of solution, in seconds.
        /// </summary>
        [DataField("evaporateTime")]
        public float EvaporateTime { get; set; } = 5f;

        /// <summary>
        ///     Name of referenced solution. Defaults to <see cref="PuddleComponent.DefaultSolutionName"/>
        /// </summary>
        [DataField("solution")]
        public string SolutionName { get; set; } = PuddleComponent.DefaultSolutionName;

        /// <summary>
        ///     Lower limit below which puddle won't evaporate. Useful when wanting to leave a stain.
        ///     Defaults to evaporate completely.
        /// </summary>
        [DataField("lowerLimit")]
        public FixedPoint2 LowerLimit = FixedPoint2.Zero;

        /// <summary>
        ///     Upper limit above which puddle won't evaporate. Useful when wanting to make sure large puddle will
        ///     remain forever. Defaults to 100.
        /// </summary>
        [DataField("upperLimit")]
        public FixedPoint2 UpperLimit = FixedPoint2.New(100); //TODO: Consider setting this back to PuddleComponent.DefaultOverflowVolume once that behaviour is fixed.

        /// <summary>
        ///     The time accumulated since the start.
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;
    }
}
