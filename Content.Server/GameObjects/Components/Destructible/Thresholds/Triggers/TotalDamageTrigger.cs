#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of total damage received
    ///     is above the specified threshold.
    /// </summary>
    [Serializable]
    public class TotalDamageTrigger : IThresholdTrigger
    {
        /// <summary>
        ///     The amount of total damage at which this threshold will trigger.
        /// </summary>
        public int Damage { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Damage, "damage", 0);
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            return damageable.TotalDamage >= Damage;
        }
    }
}
