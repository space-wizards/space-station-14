using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOnLandComponent : Component, ILand
    {
        public override string Name => "DamageOnLand";

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        [DataField("damageType")]
        private DamageType _damageType = DamageType.Blunt;

=======
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
        [DataField("amount")]
        [ViewVariables(VVAccess.ReadWrite)]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _ignoreResistances;

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt";
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;
        protected override void Initialize()
<<<<<<< HEAD
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
<<<<<<< refs/remotes/origin/master
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable)) return;

            damageable.ChangeDamage(_damageType, _amount, _ignoreResistances, eventArgs.User);
=======
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.TryChangeDamage(DamageType, _amount, _ignoreResistances);
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.TryChangeDamage(DamageType, _amount, _ignoreResistances);
>>>>>>> refactor-damageablecomponent
        }
    }
}
