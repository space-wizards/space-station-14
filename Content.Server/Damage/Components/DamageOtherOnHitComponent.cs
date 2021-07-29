using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOtherOnHitComponent : Component, IThrowCollide
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "DamageOtherOnHit";

        [DataField("damageType",required: true)]
        private readonly string _damageType = default!;

        [DataField("amount")]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.ChangeDamage(_prototypeManager.Index<DamageTypePrototype>(_damageType), _amount, _ignoreResistances, eventArgs.User);
        }
    }
}
