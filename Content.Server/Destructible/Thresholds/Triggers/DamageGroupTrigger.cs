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
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // While you're at it, maybe also combine damageGroup and damage into a dictionary, and allow it to test a sum
        // of damage types?
        [DataField("damageGroup", required: true)]
        private string _damageGroupID { get; set; } = default!;
        public DamageGroupPrototype DamageGroup => IoCManager.Resolve<IPrototypeManager>().Index<DamageGroupPrototype>(_damageGroupID);

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField("damage", required: true)]
        public int Damage { get; set; } = default!;

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
