using Content.Shared.Tabletop.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events;

/// <summary>
/// An event that is sent to the server every so often by the client to tell where an entity with a
/// <see cref="TabletopDraggableComponent"/> has been moved.
/// </summary>
[Serializable, NetSerializable]
public sealed class TabletopMoveEvent(NetEntity movedEntityUid, MapCoordinates coordinates, NetEntity tableUid)
    : EntityEventArgs
{
    /// <summary>
    /// The UID of the entity being moved.
    /// </summary>
    public NetEntity MovedEntityUid { get; } = movedEntityUid;

    /// <summary>
    /// The new coordinates of the entity being moved.
    /// </summary>
    public MapCoordinates Coordinates { get; } = coordinates;

    /// <summary>
    /// The UID of the table the entity is being moved on.
    /// </summary>
    public NetEntity TableUid { get; } = tableUid;
}
