using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Damage.Components
{
    [Friend(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public class DamageOtherOnHitComponent : Component
    {
        public override string Name => "DamageOtherOnHit";

        [DataField("damageType")]
        public DamageType DamageType { get; } = DamageType.Blunt;

        [DataField("amount")]
        public int Amount { get; } = 1;

        [DataField("ignoreResistances")]
        public bool IgnoreResistances { get; } = false;
    }
}
