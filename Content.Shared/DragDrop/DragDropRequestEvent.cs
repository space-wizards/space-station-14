using Robust.Shared.Serialization;

namespace Content.Shared.DragDrop
{
    /// <summary>
    /// Raised on the client to the server requesting a drag-drop.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class DragDropRequestEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that was dragged and dropped.
        /// </summary>
        public EntityUid Dropped { get; }

        /// <summary>
        ///     Entity that was drag dropped on.
        /// </summary>
        public EntityUid Target { get; }

        public DragDropRequestEvent(EntityUid dropped, EntityUid target)
        {
            Dropped = dropped;
            Target = target;
        }
    }
}
