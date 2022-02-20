using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Climbing
{
    public interface IClimbable { }

    public abstract class SharedClimbableComponent : Component, IClimbable, IDragDropOn
    {
        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables] [DataField("range")] protected float Range = SharedInteractionSystem.InteractionRange / 1.4f;

        public virtual bool CanDragDropOn(DragDropEvent eventArgs)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<SharedClimbingComponent>(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
