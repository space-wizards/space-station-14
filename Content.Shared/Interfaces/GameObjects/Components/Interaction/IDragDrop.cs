using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface allows the component's entity to be dragged and dropped
    ///     by mouse onto another entity and gives it behavior when that occurs.
    /// </summary>
    public interface IDragDrop
    {
        /// <summary>
        ///     Invoked server-side when this component's entity is being dragged
        ///     and dropped on another before invoking <see cref="DragDrop"/>.
        ///     Note that other drag and drop interactions may be attempted if
        ///     this one fails.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>true if <see cref="eventArgs"/> is valid, false otherwise.</returns>
        bool CanDragDrop(DragDropEventArgs eventArgs);

        /// <summary>
        ///     Invoked server-side when this component's entity is being dragged
        ///     and dropped on another.
        ///     Note that other drag and drop interactions may be attempted if
        ///     this one fails.
        /// </summary>
        /// <returns>
        ///     true if an interaction occurred and no further interaction should
        ///     be processed for this drop.
        /// </returns>
        bool DragDrop(DragDropEventArgs eventArgs);
    }

    public class DragDropEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new instance of <see cref="DragDropEventArgs"/>.
        /// </summary>
        /// <param name="user">The entity doing the drag and drop.</param>
        /// <param name="dropLocation">The location where <see cref="dropped"/> is being dropped.</param>
        /// <param name="dropped">The entity that is being dragged and dropped.</param>
        /// <param name="target">The entity that <see cref="dropped"/> is being dropped onto.</param>
        public DragDropEventArgs(IEntity user, EntityCoordinates dropLocation, IEntity dropped, IEntity target)
        {
            User = user;
            DropLocation = dropLocation;
            Dropped = dropped;
            Target = target;
        }

        /// <summary>
        ///     The entity doing the drag and drop.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The location where <see cref="Dropped"/> is being dropped.
        /// </summary>
        public EntityCoordinates DropLocation { get; }

        /// <summary>
        ///     The entity that is being dragged and dropped.
        /// </summary>
        public IEntity Dropped { get; }

        /// <summary>
        ///     The entity that <see cref="Dropped"/> is being dropped onto.
        /// </summary>
        public IEntity Target { get; }
    }
}
