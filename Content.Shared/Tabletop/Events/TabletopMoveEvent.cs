using System.Numerics;
using Content.Shared.Tabletop.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events;

/// <summary>
/// An event that is sent to the server every so often by the client to tell where an entity with a
/// <see cref="TabletopDraggableComponent"/> has been moved.
/// </summary>
[Serializable, NetSerializable]
public sealed class TabletopMoveEvent(NetEntity movedEntityUid, Vector2 position, NetEntity tableUid) : EntityEventArgs
{
    /// <summary>
    /// The UID of the entity being moved.
    /// </summary>
    public NetEntity MovedEntityUid = movedEntityUid;

    /// <summary>
    /// The new coordinates of the entity being moved, relative to the board.
    /// </summary>
    public Vector2 Position = position;

    /// <summary>
    /// The UID of the table the entity is being moved on.
    /// </summary>
    public NetEntity TableUid = tableUid;
}
