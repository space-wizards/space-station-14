using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    public interface IClimbable { };

    public abstract class SharedClimbableComponent : Component, IClimbable, IDragDropOn
    {
        public sealed override string Name => "Climbable";

        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables]
        protected float Range;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref Range, "range", SharedInteractionSystem.InteractionRange / 1.4f);
        }

        public virtual bool CanDragDropOn(DragDropEventArgs eventArgs)
        {
            return eventArgs.Dragged.HasComponent<SharedClimbingComponent>();
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
