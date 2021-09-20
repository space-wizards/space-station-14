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
        ///     The time that it will take this puddle to evaporate, in seconds.
        /// </summary>
        [DataField("evaporate_time")]
        public float EvaporateTime { get; set; } = 8f;

        [DataField("solution")] public string SolutionName { get; set; } = PuddleComponent.DefaultSolutionName;

        /// <summary>
        ///     The time accumulated since the start. Shouldn't be modified outside of EvaporationSystem.
        /// </summary>
        public float Accumulator = 0f;
    }
}
