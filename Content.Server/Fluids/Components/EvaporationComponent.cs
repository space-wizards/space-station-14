using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Reagent;
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
        ///     The time that it will take this puddle to lose one reagent unit of solution, in seconds.
        /// </summary>
        [DataField("evaporate_time")]
        public float EvaporateTime { get; set; } = 3f;

        [DataField("solution")] 
        public string SolutionName { get; set; } = PuddleComponent.DefaultSolutionName;

        [DataField("evaporation_limit")]
        public ReagentUnit EvaporationLimit = ReagentUnit.Zero;

        /// <summary>
        ///     The time accumulated since the start. Shouldn't be modified outside of EvaporationSystem.
        /// </summary>
        public float Accumulator = 0f;
    }
}
