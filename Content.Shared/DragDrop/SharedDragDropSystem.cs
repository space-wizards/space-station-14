namespace Content.Shared.DragDrop;

public abstract class SharedDragDropSystem : EntitySystem
{
    protected bool? CheckDragDropOn(DragDropEvent eventArgs)
    {
        var canDragDropOnEvent = new CanDragDropOnEvent(eventArgs.User, eventArgs.Dragged, eventArgs.Target);

        RaiseLocalEvent(eventArgs.Target, canDragDropOnEvent, false);

        return canDragDropOnEvent.Handled ? canDragDropOnEvent.CanDrop : null;
    }
}
