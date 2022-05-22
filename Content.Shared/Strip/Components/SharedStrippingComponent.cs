using Content.Shared.DragDrop;

namespace Content.Shared.Strip.Components
{
    /// <summary>
    ///     Give to an entity to say they can strip another entity.
    /// </summary>
    [RegisterComponent]
    public sealed class SharedStrippingComponent : Component, IDragDropOn
    {
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
