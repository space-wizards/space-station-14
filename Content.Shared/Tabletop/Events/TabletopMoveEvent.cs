using System;
using Content.Shared.Tabletop.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    /// <summary>
    /// An event that is sent to the server every so often by the client to tell where an entity with a
    /// <see cref="SharedTabletopDraggableComponent"/> has been moved.
    /// </summary>
    [Serializable, NetSerializable]
    public class TabletopMoveEvent : EntityEventArgs
    {
        /// <summary>
        /// The entity being moved.
        /// </summary>
        public EntityUid MovedEntity { get; }

        /// <summary>
        /// The new coordinates of the entity being moved.
        /// </summary>
        public MapCoordinates Coordinates { get; }

        public TabletopMoveEvent(EntityUid movedEntity, MapCoordinates coordinates)
        {
            MovedEntity = movedEntity;
            Coordinates = coordinates;
        }
    }
}
