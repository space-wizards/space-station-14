using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Smoking;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Friend(typeof(SmokingSystem))]
    public class SmokableComponent : Component
    {
        public override string Name => "Smokable";

        [DataField("solution")]
        public string Solution { get; } = "smokable";

        /// <summary>
        ///     Solution inhale amount per second.
        /// </summary>
        [DataField("inhaleAmount")]
        public ReagentUnit InhaleAmount { get; } = ReagentUnit.New(0.05f);

        [DataField("state")]
        public SmokableState State { get; set; } = SmokableState.Unlit;
    }
}
