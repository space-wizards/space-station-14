#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds
{
    [Serializable]
    public class Threshold : IExposeData
    {
        public bool Triggered { get; set; }

        public bool TriggersOnce { get; set; }

        [ViewVariables] public ITrigger? Trigger { get; set; }

        /// <summary>
        ///     Behaviors to activate once this threshold is triggered.
        /// </summary>
        [ViewVariables] public List<IBehavior> Behaviors { get; set; } = new();

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Triggered, "triggered", false);
            serializer.DataField(this, x => x.TriggersOnce, "triggersOnce", false);
            serializer.DataField(this, x => x.Trigger, "trigger", null);
            serializer.DataField(this, x => x.Behaviors, "behaviors", new List<IBehavior>());
        }

        public bool Reached(IDamageableComponent damageable, DestructibleSystem system)
        {
            if (Triggered && TriggersOnce)
            {
                return false;
            }

            return Trigger != null &&
                   Trigger.Reached(damageable, system);
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to get dependency and
        ///     system references from, if relevant.
        /// </param>
        public void Execute(IEntity owner, DestructibleSystem system)
        {
            Triggered = true;

            foreach (var behavior in Behaviors)
            {
                behavior.Execute(owner, system);
            }
        }
    }
}
