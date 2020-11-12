using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedClimbableComponent : Component
    {
        public sealed override string Name => "Climbable";

        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables]
        public float Range { get; protected set; }

        /// <summary>
        ///     How long it takes to climb onto this component.
        /// </summary>
        [ViewVariables]
        public float ClimbDelay { get; protected set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => Range, "range", SharedInteractionSystem.InteractionRange / 1.4f);
            serializer.DataField(this, x => ClimbDelay, "delay", 0.8f);
        }
    }
}
