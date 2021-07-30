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
        //TODO Why is damage type allowd to be null here? if it is, this component does nothing? Surely it should be a required datafield?
        //TODO PROTOTYPE Replace this code with prototype references, once they are supported.
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [DataField("damageType")]
        public string? DamageTypeID { get; set; }
        public DamageTypePrototype? DamageType => DamageTypeID == null ? null : _prototypeManager.Index<DamageTypePrototype>(DamageTypeID);

        [DataField("damage")]
        public int Damage { get; set; }

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
