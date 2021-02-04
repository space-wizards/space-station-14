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
    public class Threshold : IExposeData
    {
        private List<IBehavior> _behaviors = new();

        /// <summary>
        ///     Whether or not this threshold has already been triggered.
        /// </summary>
        [ViewVariables] public bool Triggered { get; private set; }

        /// <summary>
        ///     Whether or not this threshold only triggers once.
        ///     If false, it will trigger again once the entity is healed
        ///     and then damaged to reach this threshold once again.
        ///     It will not repeatedly trigger as damage rises beyond that.
        /// </summary>
        [ViewVariables] public bool TriggersOnce { get; set; }

        [ViewVariables] public ITrigger? Trigger { get; set; }

        /// <summary>
        ///     Behaviors to activate once this threshold is triggered.
        /// </summary>
        [ViewVariables] public IReadOnlyList<IBehavior> Behaviors => _behaviors;

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
                // The owner has been deleted. We stop execution of behaviors here.
                if (owner.Deleted)
                    return;

                behavior.Execute(owner, system);
            }
        }
    }
}
