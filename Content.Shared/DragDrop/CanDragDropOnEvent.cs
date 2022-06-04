namespace Content.Shared.DragDrop;

/// <summary>
/// Event that gets send to the target of a drag drop action
/// Mark this event as handled to specify that the entity can be dropped on
/// and set CanDrop to true or false, depending on whether dropping the entity onto the target is actually possible.
/// </summary>
public sealed class CanDragDropOnEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     Entity doing the drag and drop.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     Entity that is being dragged.
    /// </summary>
    public EntityUid Dragged { get; }

    /// <summary>
    ///     Entity that is being dropped on.
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    ///     If the dragged entity can be dropped on the target.
    /// </summary>
    public bool CanDrop { get; set; } = false;

    public CanDragDropOnEvent(EntityUid user, EntityUid dragged, EntityUid target)
    {
        User = user;
        Dragged = dragged;
        Target = target;
    }
}
