#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     If all damage types received are above the threshold, at least
    ///     one of their old values in <see cref="PreviousDamage"/> was below the
    ///     threshold and at least one damage type has increased in value this
    ///     trigger will activate.
    /// </summary>
    [Serializable]
    public class TotalDamageTypesTrigger : ITrigger
    {
        /// <summary>
        ///     The amount of damage at which this trigger was last reached.
        /// </summary>
        private Dictionary<DamageType, int> PreviousDamage { get; set; } = new();

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        ///     The damage requirements of all <see cref="DamageType"/> must be met.
        /// </summary>
        private Dictionary<DamageType, int> Damage { get; set; } = new();

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Damage, "damage", new Dictionary<DamageType, int>());
        }

        private bool ReachedInternal(IDamageableComponent damageable)
        {
            var anyIncreased = false;
            var anyOldUnderThreshold = false;

            foreach (var (type, damageRequired) in Damage)
            {
                if (!damageable.TryGetDamage(type, out var damageReceived))
                {
                    return false;
                }

                if (damageReceived < damageRequired)
                {
                    return false;
                }

                if (!PreviousDamage.TryGetValue(type, out var previousDamage))
                {
                    previousDamage = 0;
                }

                if (damageReceived > previousDamage)
                {
                    anyIncreased = true;
                }

                if (previousDamage < damageRequired)
                {
                    anyOldUnderThreshold = true;
                }
            }

            return anyIncreased && anyOldUnderThreshold;
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            var reached = ReachedInternal(damageable);

            PreviousDamage.Clear();

            foreach (var (@class, damageReceived) in damageable.DamageTypes)
            {
                PreviousDamage[@class] = damageReceived;
            }

            return reached;
        }
    }
}
