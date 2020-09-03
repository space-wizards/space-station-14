using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    public class DragDropEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new instance of <see cref="DragDropEventArgs"/>.
        /// </summary>
        /// <param name="user">The entity doing the drag and drop.</param>
        /// <param name="dropLocation">The location where <see cref="dropped"/> is being dropped.</param>
        /// <param name="dropped">The entity that is being dragged and dropped.</param>
        /// <param name="target">The entity that <see cref="dropped"/> is being dropped onto.</param>
        public DragDropEventArgs(IEntity user, GridCoordinates dropLocation, IEntity dropped, IEntity target)
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
        public GridCoordinates DropLocation { get; }

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
