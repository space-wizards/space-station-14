using Robust.Shared.GameObjects;

namespace Content.Shared.DragDrop;

public class SharedDragDropSystem : EntitySystem
{
    protected bool? CheckDragDropOn(DragDropEvent eventArgs)
    {
        var canDragDropOnEvent = new CanDragDropOnEvent(eventArgs.User, eventArgs.Dragged, eventArgs.Target);

        RaiseLocalEvent(eventArgs.Target, canDragDropOnEvent, false);

        return canDragDropOnEvent.Handled ? canDragDropOnEvent.CanDrop : null;
    }
}
