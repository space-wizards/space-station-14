using System;
using Content.Shared.Tabletop.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
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
        public NetUserId DraggingPlayer { get; }
        public EntityUid MovedEntity { get; }
        public MapCoordinates Coordinates { get; }
        public bool FirstDrag { get; }

        /**
         * <param name="draggingPlayer">The player dragging the piece.</param>
         * <param name="movedEntity">The entity being moved.</param>
         * <param name="coordinates">The new coordinates of the entity being moved.</param>
         * <param name="firstDrag">Whether this is the first tick we are dragging this entity.</param>
         */
        public TabletopMoveEvent(NetUserId draggingPlayer, EntityUid movedEntity, MapCoordinates coordinates, bool firstDrag)
        {
            DraggingPlayer = draggingPlayer;
            MovedEntity = movedEntity;
            Coordinates = coordinates;
            FirstDrag = firstDrag;
        }
    }
}
