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

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [DataField("damageType", required: true)]
        private readonly string _damageType = default!;

        [DataField("amount")]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable))
                return;
            damageable.ChangeDamage(_prototypeManager.Index<DamageTypePrototype>(_damageType), _amount, _ignoreResistances, eventArgs.User);
        }
    }
}
