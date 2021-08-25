using System;
using Content.Shared.Damage;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Content.Shared.Damage.Prototypes;

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
        [DataField("damageType", required:true)]
        public string _damageTypeID { get; set; } = default!;
        public DamageTypePrototype DamageType => IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);

        [DataField("damage", required: true)]
        public int Damage { get; set; } = default!;

        public bool Reached(DamageableComponent damageable, DestructibleSystem system)
        {
            return damageable.DamagePerType.TryGetValue(DamageType, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
