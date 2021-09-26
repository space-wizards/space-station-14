using Content.Shared.Smoking;
using Content.Shared.Sound;
using Content.Shared.Temperature;
using Content.Server.Items;
using Content.Server.Light.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    [RegisterComponent]
    [Friend(typeof(MatchstickSystem))]
    public class MatchstickComponent : Component
    {
        public override string Name => "Matchstick";

        /// <summary>
        /// Current state to matchstick. Can be <code>Unlit</code>, <code>Lit</code> or <code>Burnt</code>.
        /// </summary>
        [ViewVariables]
        public SharedBurningStates CurrentState = SharedBurningStates.Unlit;

        /// <summary>
        /// How long will matchstick last in seconds.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("duration")]
        public int Duration = 10;

        /// <summary>
        /// Sound played when you ignite the matchstick.
        /// </summary>
        [DataField("igniteSound", required: true)] public SoundSpecifier IgniteSound = default!;

        /// <summary>
        /// Point light component. Gives matches a glow in dark effect.
        /// </summary>
        [ComponentDependency]
        public readonly PointLightComponent? PointLightComponent = default!;
    }
}
