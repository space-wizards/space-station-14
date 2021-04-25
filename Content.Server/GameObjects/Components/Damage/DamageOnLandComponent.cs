using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class DamageOnLandComponent : Component, ILand
    {
        public override string Name => "DamageOnLand";

        [DataField("damageType")]
        private DamageTypePrototype _damageType = default!;

        [DataField("amount")]
        private int _amount = 1;

        [DataField("ignoreResistances")]
        private bool _ignoreResistances;

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable)) return;

            if (_damageType is null)
            {
                damageable.ChangeDamage(damageable.GetDamageType("Blunt"), _amount, _ignoreResistances, eventArgs.User);
            }
            else
            {
                damageable.ChangeDamage(_damageType, _amount, _ignoreResistances, eventArgs.User);
            }
        }
    }
}
