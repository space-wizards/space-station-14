#nullable enable
using System;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified type is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageTypeTrigger : IThresholdTrigger
    {
        [DataField("type")]
        public DamageType? Type { get; set; }

        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (Type == null)
            {
                return false;
            }

            return damageable.TryGetDamage(Type.Value, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
