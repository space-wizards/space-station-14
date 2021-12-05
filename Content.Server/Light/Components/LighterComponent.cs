using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Lighter
{
    [RegisterComponent]
    [Friend(typeof(LighterSystem))]
    public class LighterComponent : Component
    {
        public override string Name => "Lighter";

        /// <summary>
        /// Current state to lighter. Can be <code>false</code>, <code>true</code>.
        /// </summary>
        [ViewVariables]
        public bool Lit = false;

        /// <summary>
        /// Sound played when you ignite the lighter.
        /// </summary>
        [DataField("igniteSound", required: true)] public SoundSpecifier IgniteSound = default!;

        /// <summary>
        /// Point light component. Gives lighter a glow in dark effect.
        /// </summary>
        [ComponentDependency]
        public readonly PointLightComponent? PointLightComponent = default!;
    }
}
