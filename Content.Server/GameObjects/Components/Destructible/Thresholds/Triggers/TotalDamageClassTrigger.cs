using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    [Serializable]
    public class TotalDamageClassTrigger : ITrigger
    {
        private Dictionary<DamageClass, int> PreviousDamage { get; set; }

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        ///     The damage requirements of all <see cref="DamageClass"/> must be met.
        /// </summary>
        private Dictionary<DamageClass, int> Damage { get; set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.PreviousDamage, "previousDamage", new Dictionary<DamageClass, int>());
            serializer.DataField(this, x => x.Damage, "damage", new Dictionary<DamageClass, int>());
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            var anyIncreased = false;

            foreach (var (@class, damageRequired) in Damage)
            {
                if (!damageable.TryGetDamage(@class, out var damageReceived))
                {
                    return false;
                }

                if (!PreviousDamage.TryGetValue(@class, out var previousDamage) ||
                    damageReceived > previousDamage && previousDamage < damageRequired)
                {
                    anyIncreased = true;
                    break;
                }
            }

            PreviousDamage.Clear();

            foreach (var (@class, damageReceived) in damageable.DamageClasses)
            {
                PreviousDamage[@class] = damageReceived;
            }

            return anyIncreased;
        }
    }
}
