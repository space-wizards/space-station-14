using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    [Serializable]
    public class TotalDamageTrigger : ITrigger
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
