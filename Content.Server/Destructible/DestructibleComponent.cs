using System.Collections.Generic;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Destructible
{
    /// <summary>
    ///     When attached to an <see cref="Robust.Shared.GameObjects.EntityUid"/>, allows it to take damage
    ///     and triggers thresholds when reached.
    /// </summary>
    [RegisterComponent]
    public class DestructibleComponent : Component
    {
        public override string Name => "Destructible";

        [ViewVariables]
        [DataField("thresholds")]
        public List<DamageThreshold> Thresholds = new();

    }
}
