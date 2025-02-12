using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SmokableComponent : Component
    {
        [DataField]
        public string Solution { get; private set; } = "smokable";

        /// <summary>
        ///     Solution inhale amount per second.
        /// </summary>
        [DataField, ViewVariables]
        public FixedPoint2 InhaleAmount { get; private set; } = FixedPoint2.New(0.05f);

        [DataField]
        public SmokableState State { get; set; } = SmokableState.Unlit;

        [DataField, ViewVariables]
        public float ExposeTemperature { get; set; } = 0;

        [DataField, ViewVariables]
        public float ExposeVolume { get; set; } = 1f;

        // clothing prefixes
        [DataField]
        public string BurntPrefix = "unlit";
        [DataField]
        public string LitPrefix = "lit";
        [DataField]
        public string UnlitPrefix = "unlit";
    }
}
