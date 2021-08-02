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
        public override string Name => "DamageOtherOnHit";

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [DataField("damageType", required: true)]
        private readonly string _damageTypeID = default!;
        private DamageTypePrototype _damageType => _prototypeManager.Index<DamageTypePrototype>(_damageTypeID);

        [DataField("amount")]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.TryChangeDamage(_damageType, _amount, _ignoreResistances, eventArgs.User);
        }
    }
}
