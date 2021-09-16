using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    public sealed class EvaporationComponent : Component
    {
        public override string Name => "Evaporation";

        /// <summary>
        ///     The time that it will take this puddle to evaporate, in seconds.
        /// </summary>
        [DataField("evaporate_time")]
        public float EvaporateTime { get; private set; } = 5f;

        /// <summary>
        ///      How few <see cref="ReagentUnit"/> we can hold prior to self-destructing
        /// </summary>
        [DataField("evaporate_threshold")]
        public ReagentUnit EvaporateThreshold = ReagentUnit.New(20);
    }
}
