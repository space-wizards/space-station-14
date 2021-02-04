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
    ///     A trigger that will activate when all of the damage classes received
    ///     are above the specified threshold.
    /// </summary>
    [Serializable]
    public class TotalDamageClassesTrigger : IThresholdTrigger
    {
        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        ///     The damage requirements of all <see cref="DamageClass"/> must be met.
        /// </summary>
        private Dictionary<DamageClass, int> Damage { get; set; } = new();

        public void ExposeData(ObjectSerializer serializer)
        {
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
