using Content.Shared.Smoking;
using Content.Server.Light.EntitySystems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Content.Shared.Light;

namespace Content.Server.Light.Components
{
    [RegisterComponent]
    [Friend(typeof(CandleSystem))]

    public class CandleComponent : Component
    {
        public override string Name => "Candle";

        /// <summary>
        /// Current state to matchstick. Can be <code>Unlit</code>, <code>Lit</code> or <code>Burnt</code>.
        /// </summary>
        [ViewVariables]
        public SmokableState CurrentState = SmokableState.Unlit;


        /// <summary>
        /// State enum for determing what icon to use for the candle. There are 4 total states, BrandNew, Half, AlmostOut, Dead.
        /// </summary>
        [ViewVariables]
        public CandleState CurrentCandleIcon = CandleState.BrandNew;


        /// <summary>
        /// Whether or not this is the first light of this candle. We use this to determine if we should initialize the candle before ignition
        /// </summary>
        [ViewVariables]
        public bool IsFirstLight = true;

        /// <summary>
        /// LightBehaviour behaviour ID. Used to trigger a specific light animation based on candle state
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("brandNewBehaviorID")]
        public string BrandNewBehaviourID { get; set; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("halfNewBehaviourID")]
        public string HalfNewBehaviorID { get; set; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("almostOutBehaviourID")]
        public string AlmostOutBehaviourID { get; set; } = string.Empty;

        /// <summary>
        /// How long will the candle last in seconds. This is tracked and subtracted from whilst the candle is lit
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("wax")]
        public float WaxLeft = 10;

        [ViewVariables(VVAccess.ReadOnly)]
        public float WaxTotal = 10;

        /// <summary>
        /// Point light component so the candle can actually produce light
        /// </summary>
        [ComponentDependency]
        public readonly PointLightComponent? PointLightComponent = default!;
    }
}
