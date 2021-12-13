using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Strip.Components
{
    /// <summary>
    ///     Give to an entity to say they can strip another entity.
    /// </summary>
    [RegisterComponent]
    public class SharedStrippingComponent : Component, IDragDropOn
    {
        public override string Name => "Stripping";

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(eventArgs.Dragged, out SharedStrippableComponent? strippable)) return false;
            return strippable.CanBeStripped(Owner);
        }

        bool IDragDropOn.DragDropOn(DragDropEvent eventArgs)
        {
            // Handled by StrippableComponent
            return true;
        }
    }
}
