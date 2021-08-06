using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    [Serializable, NetSerializable]
    public class TabletopDraggingPlayerChangedEvent : EntityEventArgs
    {
        public EntityUid DraggedEntityUid;
        public NetUserId? DraggingPlayer;

        /**
         * <summary>
         * Event to tell other clients that we are dragging this item. Necessery to handle multiple users
         * trying to move a single item at the same time.
         * </summary>
         * <param name="draggedEntityUid">The UID of the entity being dragged.</param>
         * <param name="draggingPlayer">The NetUserID of the player that is now dragging the item.</param>
         */
        public TabletopDraggingPlayerChangedEvent(EntityUid draggedEntityUid, NetUserId? draggingPlayer)
        {
            DraggedEntityUid = draggedEntityUid;
            DraggingPlayer = draggingPlayer;
        }
    }
}
