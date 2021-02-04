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
    ///     If all damage classes received are above the threshold, at least
    ///     one of their old values in <see cref="PreviousDamage"/> was below the
    ///     threshold and at least one damage class has increased in value this
    ///     trigger will activate.
    /// </summary>
    [Serializable]
    public class TotalDamageClassesTrigger : ITrigger
    {
        /// <summary>
        ///     The amount of damage at which this trigger was last reached.
        /// </summary>
        private Dictionary<DamageClass, int> PreviousDamage { get; set; } = new();

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        ///     The damage requirements of all <see cref="DamageClass"/> must be met.
        /// </summary>
        private Dictionary<DamageClass, int> Damage { get; set; } = new();

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.PreviousDamage, "previousDamage", new Dictionary<DamageClass, int>());
            serializer.DataField(this, x => x.Damage, "damage", new Dictionary<DamageClass, int>());
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
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
            }

            return true;
        }
    }
}
