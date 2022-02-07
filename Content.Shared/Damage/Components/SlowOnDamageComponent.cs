using System.Collections.Generic;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage.Components
{
    // TODO It'd be nice if this could be a destructible threshold, but on the other hand,
    // that doesn't really work with events at all, and
    [RegisterComponent, NetworkedComponent]
    public class SlowOnDamageComponent : Component
    {
        /// <summary>
        ///     Damage -> movespeed dictionary. This is -damage-, not -health-.
        /// </summary>
        [DataField("speedModifierThresholds", required: true)]
        public readonly Dictionary<FixedPoint2, float> SpeedModifierThresholds = default!;
    }
}
