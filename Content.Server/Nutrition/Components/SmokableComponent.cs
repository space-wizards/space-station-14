using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Friend(typeof(SmokingSystem))]
    public class SmokableComponent : Component
    {
        [DataField("solution")]
        public string Solution { get; } = "smokable";

        /// <summary>
        ///     Solution inhale amount per second.
        /// </summary>
        [DataField("inhaleAmount")]
        public FixedPoint2 InhaleAmount { get; } = FixedPoint2.New(0.05f);

        [DataField("state")]
        public SmokableState State { get; set; } = SmokableState.Unlit;

        [DataField("exposeTemperature")]
        public float ExposeTemperature { get; set; } = 0;

        [DataField("exposeVolume")]
        public float ExposeVolume { get; set; } = 1f;

        // clothing prefixes
        [DataField("burntPrefix")]
        public string BurntPrefix = "unlit";
        [DataField("litPrefix")]
        public string LitPrefix = "lit";
        [DataField("unlitPrefix")]
        public string UnlitPrefix = "unlit";
    }
}
