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
        public NetEntity Dragged { get; }

        /// <summary>
        ///     Entity that was drag dropped on.
        /// </summary>
        public NetEntity Target { get; }

        public DragDropRequestEvent(NetEntity dragged, NetEntity target)
        {
            Dragged = dragged;
            Target = target;
        }
    }
}
