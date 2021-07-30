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
    ///     of the specified class is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageGroupTrigger : IThresholdTrigger
    {

        //TODO QUESTION Why is damage type allowd to be null here? if it is, this component does nothing? Surely it should be a required datafield?
        //TODO PROTOTYPE Replace this code with prototype references, once they are supported.
        [DataField("damageGroup")]
        public string? DamageGroupID { get; set; }
        public DamageTypePrototype? DamageGroup => DamageGroupID == null ? null : IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(DamageGroupID);

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (DamageGroup == null)
            {
                return false;
            }

            return damageable.TryGetDamage(DamageGroup, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
