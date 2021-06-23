#nullable enable
using System;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified class is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageClassTrigger : IThresholdTrigger
    {
        /// <summary>
        ///     The class to check the damage of.
        /// </summary>
        [DataField("class")]
        public DamageClass? Class { get; set; }

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (Class == null)
            {
                return false;
            }

            return damageable.TryGetDamage(Class.Value, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
