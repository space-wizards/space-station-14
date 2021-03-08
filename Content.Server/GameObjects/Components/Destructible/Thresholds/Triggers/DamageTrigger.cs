#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageTrigger : IThresholdTrigger
    {
        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            return damageable.TotalDamage >= Damage;
        }
    }
}
