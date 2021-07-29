using System;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [DataField("damageType")]
        public string? DamageType { get; set; }

        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (DamageType == null)
            {
                return false;
            }

            return damageable.TryGetDamage(_prototypeManager.Index<DamageTypePrototype>(DamageType), out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
