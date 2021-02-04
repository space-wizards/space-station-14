using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    [Serializable]
    public class TotalDamageTypeTrigger : ITrigger
    {
        /// <summary>
        ///     The amount of damage at which this trigger was last reached.
        /// </summary>
        private Dictionary<DamageType, int> PreviousDamage { get; set; }

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        ///     The damage requirements of all <see cref="DamageType"/> must be met.
        /// </summary>
        private Dictionary<DamageType, int> Damage { get; set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Damage, "damage", new Dictionary<DamageType, int>());
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            var anyIncreased = false;

            foreach (var (type, damageRequired) in Damage)
            {
                if (!damageable.TryGetDamage(type, out var damageReceived))
                {
                    return false;
                }

                if (!anyIncreased &&
                    PreviousDamage.TryGetValue(type, out var previousDamage) &&
                    damageReceived > previousDamage)
                {
                    anyIncreased = true;
                }

                if (damageReceived < damageRequired)
                {
                    return false;
                }
            }

            PreviousDamage.Clear();

            foreach (var (@class, damageReceived) in damageable.DamageTypes)
            {
                PreviousDamage[@class] = damageReceived;
            }

            return anyIncreased;
        }
    }
}
