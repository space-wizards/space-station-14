using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOtherOnHitComponent : Component, IThrowCollide
    {
        public override string Name => "DamageOtherOnHit";

        [DataField("damageType")]
        private DamageType _damageType = DamageType.Blunt;
        [DataField("amount")]
        private int _amount = 1;
        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable)) return;

            damageable.ChangeDamage(_damageType, _amount, _ignoreResistances, eventArgs.User);
        }
    }
}
