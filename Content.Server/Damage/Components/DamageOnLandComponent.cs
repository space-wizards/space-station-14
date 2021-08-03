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
    public class DamageOnLandComponent : Component, ILand
    {
        public override string Name => "DamageOnLand";

        [DataField("amount")]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt";
        private DamageTypePrototype _damageType = default!;
        protected override void Initialize()
        {
            base.Initialize();
            _damageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.TryChangeDamage(_damageType, _amount, _ignoreResistances);
        }
    }
}
