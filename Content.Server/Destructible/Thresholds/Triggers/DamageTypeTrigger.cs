using System;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified type is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageTypeTrigger : IThresholdTrigger
    {
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // While you're at it, maybe also combine damageGroup and damage into a dictionary, and allow it to test a sum
        // of damage types?
        [DataField("damageType", required:true)]
        public string _damageTypeID { get; set; } = default!;
        public DamageTypePrototype DamageType => IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);

        [DataField("damage", required: true)]
        public int Damage { get; set; } = default!;

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (DamageType == null)
            {
                return false;
            }

            return damageable.TryGetDamage(DamageType, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
