using Content.Shared.Tabletop.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    /// <summary>
    /// An event that is sent to the server every so often by the client to tell where an entity with a
    /// <see cref="TabletopDraggableComponent"/> has been moved.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class TabletopMoveEvent : EntityEventArgs
    {
        /// <summary>
        /// The UID of the entity being moved.
        /// </summary>
        public EntityUid MovedEntityUid { get; }

        /// <summary>
        /// The new coordinates of the entity being moved.
        /// </summary>
        public MapCoordinates Coordinates { get; }

        /// <summary>
        /// The UID of the table the entity is being moved on.
        /// </summary>
        public EntityUid TableUid { get; }

        public TabletopMoveEvent(EntityUid movedEntityUid, MapCoordinates coordinates, EntityUid tableUid)
        {
            MovedEntityUid = movedEntityUid;
            Coordinates = coordinates;
            TableUid = tableUid;
        }
    }
}
