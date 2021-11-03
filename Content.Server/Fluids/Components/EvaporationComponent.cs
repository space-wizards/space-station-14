using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    [Friend(typeof(EvaporationSystem))]
    public sealed class EvaporationComponent : Component
    {
        public override string Name => "Evaporation";

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
        ///     Upper limit below which puddle won't evaporate. Useful when wanting to make sure large puddle will
        ///     remain forever. Defaults to <see cref="PuddleComponent.DefaultOverflowVolume"/>.
        /// </summary>
        [DataField("upperLimit")]
        public FixedPoint2 UpperLimit = PuddleComponent.DefaultOverflowVolume;

        /// <summary>
        ///     The time accumulated since the start.
        /// </summary>
        public float Accumulator = 0f;
    }
}
