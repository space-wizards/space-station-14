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
    public sealed class MatchstickComponent : Component
    {
        /// <summary>
        /// Current state to matchstick. Can be <code>Unlit</code>, <code>Lit</code> or <code>Burnt</code>.
        /// </summary>
        [ViewVariables]
        public SmokableState CurrentState = SmokableState.Unlit;

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
    }
}
