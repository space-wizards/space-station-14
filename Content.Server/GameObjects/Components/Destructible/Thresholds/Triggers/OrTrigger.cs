#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when any of its triggers have activated.
    /// </summary>
    [Serializable]
    public class OrTrigger : IThresholdTrigger
    {
        public List<IThresholdTrigger> Triggers { get; } = new();

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Triggers, "triggers", new List<IThresholdTrigger>());
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            foreach (var trigger in Triggers)
            {
                if (trigger.Reached(damageable, system))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
