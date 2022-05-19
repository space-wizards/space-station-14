using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.DragDrop
{
    /// <summary>
    /// Requests a drag / drop interaction to be performed
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class DragDropRequestEvent : EntityEventArgs
    {
        /// <summary>
        ///     Location that the entity was dropped.
        /// </summary>
        public EntityCoordinates DropLocation { get; }

        /// <summary>
        ///     Entity that was dragged and dropped.
        /// </summary>
        public EntityUid Dropped { get; }

        /// <summary>
        ///     Entity that was drag dropped on.
        /// </summary>
        public EntityUid Target { get; }

        public DragDropRequestEvent(EntityCoordinates dropLocation, EntityUid dropped, EntityUid target)
        {
            DropLocation = dropLocation;
            Dropped = dropped;
            Target = target;
        }
    }
}
