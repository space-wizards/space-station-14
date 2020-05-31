using System;
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
        /// Invoked when this component's entity is being dropped after being dragged by the mouse.
        /// Note that Target may be null if the entity is dropped on nothing in particular
        /// Make sure to validate if this object can be dropped on the target object!
        /// </summary>
        /// <returns>true iff an interaction occurred and no further interaction should
        /// be processed for this drop.</returns>
        bool DragDrop(DragDropEventArgs eventArgs);
    }

    public class DragDropEventArgs : EventArgs
    {
        public DragDropEventArgs(IEntity user, GridCoordinates clickLocation, IEntity dropped, IEntity target)
        {
            User = user;
            DropLocation = clickLocation;
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
