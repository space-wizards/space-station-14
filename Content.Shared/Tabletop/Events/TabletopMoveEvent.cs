using System;
using Content.Shared.Tabletop.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    /**
     * <summary>
     * An event that is sent to the server every so often by the client to tell where an entity with a
     * <see cref="SharedTabletopDraggableComponent"/> has been moved.
     * </summary>
     */
    [Serializable, NetSerializable]
    public class TabletopMoveEvent : EntityEventArgs
    {
        public EntityUid MovedEntity { get; }
        public MapCoordinates Coordinates { get; }

        /**
         * <param name="movedEntity">The entity being moved.</param>
         * <param name="coordinates">The new coordinates of the entity being moved.</param>
         */
        public TabletopMoveEvent(EntityUid movedEntity, MapCoordinates coordinates)
        {
            MovedEntity = movedEntity;
            Coordinates = coordinates;
        }
    }
}
