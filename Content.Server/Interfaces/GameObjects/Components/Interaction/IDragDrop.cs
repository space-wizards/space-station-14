using System;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    /// This interface allows the component's entity to be dragged and dropped by mouse and gives it
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

        /// <summary>
        /// Can be null if there is no target entity
        /// </summary>
        public IEntity Target { get; }
        //TODO: Is this needed? Can it really be null?
        public bool HasTarget => Target != null;
    }
}
