#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds
{
    public class Threshold : IExposeData
    {
        /// <summary>
        ///     Whether or not this threshold has already been triggered.
        /// </summary>
        [ViewVariables] public bool Triggered;

        /// <summary>
        ///     Whether or not this threshold only triggers once.
        ///     If false, it will trigger again once the entity is healed
        ///     and then damaged to reach this threshold once again.
        ///     It will not repeatedly trigger as damage rises beyond that.
        /// </summary>
        [ViewVariables] public bool TriggersOnce;

        /// <summary>
        ///     Behaviors to activate once this threshold is triggered.
        /// </summary>
        [ViewVariables] public List<IThresholdBehavior> Behaviors = new();

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Triggered, "triggered", false);
            serializer.DataField(ref TriggersOnce, "triggersOnce", false);
            serializer.DataField(ref Behaviors, "behaviors", new List<IThresholdBehavior>());
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to get dependency and
        ///     system references from, if relevant.
        /// </param>
        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            Triggered = true;

            foreach (var behavior in Behaviors)
            {
                behavior.Trigger(owner, system);
            }
        }
    }
}
