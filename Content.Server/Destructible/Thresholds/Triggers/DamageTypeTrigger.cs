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
<<<<<<< refs/remotes/origin/master
        [DataField("type")]
        public DamageType? Type { get; set; }
=======
        [DataField("damageType")]
        public string? DamageType { get; set; }
>>>>>>> update damagecomponent across shared and server

        [DataField("damage")]
        public int Damage { get; set; }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (DamageType == null)
            {
                return false;
            }

<<<<<<< refs/remotes/origin/master
            return damageable.TryGetDamage(Type.Value, out var damageReceived) &&
=======
            return damageable.TryGetDamage(damageable.GetDamageType(DamageType), out var damageReceived) &&
>>>>>>> update damagecomponent across shared and server
                   damageReceived >= Damage;
        }
    }
}
