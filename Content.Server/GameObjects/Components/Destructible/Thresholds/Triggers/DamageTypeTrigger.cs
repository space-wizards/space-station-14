#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified type is above the specified threshold.
    /// </summary>
    [Serializable]
    public class DamageTypeTrigger : IThresholdTrigger
    {
        public DamageType? Type { get; set; }

        public int Damage { get; set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Type, "type", null);
            serializer.DataField(this, x => x.Damage, "damage", 0);
        }

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
