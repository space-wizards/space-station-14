using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    /// <summary>
    /// Requests a drag / drop interaction to be performed
    /// </summary>
    [Serializable, NetSerializable]
    public class DragDropMessage : EntitySystemMessage
    {
        public EntityCoordinates DropLocation { get; }
        public EntityUid Dropped { get; }
        public EntityUid Target { get; }

        public DragDropMessage(EntityCoordinates dropLocation, EntityUid dropped, EntityUid target)
        {
            DropLocation = dropLocation;
            Dropped = dropped;
            Target = target;
        }
    }
}
