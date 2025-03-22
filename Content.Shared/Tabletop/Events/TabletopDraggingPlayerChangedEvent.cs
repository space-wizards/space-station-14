using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events;

/// <summary>
/// Event to tell other clients that we are dragging this item. Necessery to handle multiple users
/// trying to move a single item at the same time.
/// </summary>
[Serializable, NetSerializable]
public sealed class TabletopDraggingPlayerChangedEvent(NetEntity draggedEntityUid, bool isDragging) : EntityEventArgs
{
    /// <summary>
    /// The UID of the entity being dragged.
    /// </summary>
    public NetEntity DraggedEntityUid = draggedEntityUid;

    public bool IsDragging = isDragging;
}
