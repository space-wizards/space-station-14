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
    ///     of the specified class is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageClassTrigger : IThresholdTrigger
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     The class to check the damage of.
        /// </summary>
        [DataField("class")]
        public string? Group { get; set; }

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (Group == null)
            {
                return false;
            }

            return damageable.TryGetDamage(_prototypeManager.Index<DamageGroupPrototype>(Group), out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
