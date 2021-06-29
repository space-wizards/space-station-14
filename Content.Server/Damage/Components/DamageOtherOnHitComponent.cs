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

<<<<<<< refs/remotes/origin/master
        [DataField("damageType")]
        public DamageType DamageType { get; } = DamageType.Blunt;
=======
        [DataField("damageType",required: true)]
        private readonly string _damageType = default!;
>>>>>>> update damagecomponent across shared and server

        [DataField("amount")]
        public int Amount { get; } = 1;

        [DataField("ignoreResistances")]
<<<<<<< refs/remotes/origin/master
        public bool IgnoreResistances { get; } = false;
=======
        private bool _ignoreResistances;

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.ChangeDamage(damageable.GetDamageType(_damageType), _amount, _ignoreResistances, eventArgs.User);
        }
>>>>>>> update damagecomponent across shared and server
    }
}
