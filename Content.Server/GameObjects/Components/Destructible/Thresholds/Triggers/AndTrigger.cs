#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when all of its triggers have activated.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class AndTrigger : IThresholdTrigger
    {
        [DataField("triggers")]
        public List<IThresholdTrigger> Triggers { get; set; } = new();

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            foreach (var trigger in Triggers)
            {
                if (!trigger.Reached(damageable, system))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
