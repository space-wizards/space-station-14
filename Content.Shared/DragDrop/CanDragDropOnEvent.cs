using Robust.Shared.GameObjects;

namespace Content.Shared.DragDrop;

public class CanDragDropOnEvent : HandledEntityEventArgs
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
