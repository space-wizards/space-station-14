using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    /// <summary>
    /// Event to tell other clients that we are dragging this item. Necessery to handle multiple users
    /// trying to move a single item at the same time.
    /// </summary>
    [Serializable, NetSerializable]
    public class TabletopDraggingPlayerChangedEvent : EntityEventArgs
    {
        /// <summary>
        /// The UID of the entity being dragged.
        /// </summary>
        public EntityUid DraggedEntityUid;

        /// <summary>
        /// The NetUserID of the player that is now dragging the item.
        /// </summary>
        public NetUserId? DraggingPlayer;

        public TabletopDraggingPlayerChangedEvent(EntityUid draggedEntityUid, NetUserId? draggingPlayer)
        {
            DraggedEntityUid = draggedEntityUid;
            DraggingPlayer = draggingPlayer;
        }
    }
}
