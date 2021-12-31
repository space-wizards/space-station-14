using Content.Shared.Smoking;
using Content.Shared.Sound;
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
        public SmokableState CurrentState = SmokableState.Unlit;

        /// <summary>
        /// Point light component. Gives matches a glow in dark effect.
        /// </summary>
        [ComponentDependency]
        public readonly PointLightComponent? PointLightComponent = default!;
    }
}
