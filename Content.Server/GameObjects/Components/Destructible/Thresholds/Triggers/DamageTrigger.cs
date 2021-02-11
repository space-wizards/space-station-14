#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     is above the specified threshold.
    /// </summary>
    [Serializable]
    public class DamageTrigger : IThresholdTrigger
    {
        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        public int Damage { get; set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Damage, "damage", 0);
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            return damageable.TotalDamage >= Damage;
        }
    }
}
