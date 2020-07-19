using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// This interface allows the component's entity to be dragged and dropped by mouse onto another entity and gives it
    /// behavior when that occurs.
    /// </summary>
    public interface IDragDrop
    {
        /// <summary>
        /// Invoked server-side when this component's entity is being dragged and dropped on another.
        ///
        /// There is no other server-side drag and drop check other than a range check, so make sure to validate
        /// if this object can be dropped on the target object!
        /// </summary>
        /// <returns>true iff an interaction occurred and no further interaction should
        /// be processed for this drop.</returns>
        bool DragDrop(DragDropEventArgs eventArgs);
    }

    public class DragDropEventArgs : EventArgs
    {
        public DragDropEventArgs(IEntity user, GridCoordinates dropLocation, IEntity dropped, IEntity target)
        {
            User = user;
            DropLocation = dropLocation;
            Dropped = dropped;
            Target = target;
        }

        public IEntity User { get; }
        public GridCoordinates DropLocation { get; }
        public IEntity Dropped { get; }
        public IEntity Target { get; }
    }
}
