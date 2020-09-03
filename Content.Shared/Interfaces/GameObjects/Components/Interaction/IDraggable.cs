using System;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface allows a local client to initiate dragging of the component's
    ///     entity by mouse, for drag and drop interactions.
    /// </summary>
    public interface IDraggable
    {
        /// <summary>
        ///     Invoked when an user is attempting to initiate a drag with
        ///     this component's entity in range. It's fine to return true even if there
        ///     wouldn't be any valid targets - just return true if this entity is in a
        ///     "draggable" state.
        /// </summary>
        /// <param name="args">
        ///     The information about the drag, such as who is doing it.
        /// </param>
        /// <returns>True if the drag should be initiated.</returns>
        bool CanBeDragged(CanDragEventArgs args)
        {
            return true;
        }

        /// <summary>
        ///     Invoked on entities visible to the user to check if this component's
        ///     entity can be dropped on the indicated target entity.
        ///     No need to check range / reachability in here.
        /// </summary>
        /// <returns>
        ///     True if target is a valid target to be dropped on by this component's
        ///     entity. Returning true will cause the target entity to be highlighted as
        ///     a potential target and allow dropping when in range.
        /// </returns>
        bool CanDrop(CanDropEventArgs args);

        /// <summary>
        ///     Invoked when this component's entity is being dragged
        ///     and dropped on another before invoking <see cref="Dropped"/>.
        ///     Note that other drag and drop interactions may be attempted if
        ///     this one fails.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>true if <see cref="args"/> is valid, false otherwise.</returns>
        // bool CanDragDrop(DragDropEventArgs args)
        // {
        //     return true;
        // }

        /// <summary>
        ///     Invoked when this component's entity is being dropped on another.
        ///     Other drag and drop interactions may be attempted if this one fails.
        /// </summary>
        /// <param name="args">
        ///     The information about the drag, such as who is doing it.
        /// </param>
        /// <returns>
        ///     True if an interaction occurred and no further interaction should be processed for this drop.
        /// </returns>
        bool Dropped(DragDropEventArgs args);
    }

    public class CanDropEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new instance of <see cref="CanDropEventArgs"/>.
        /// </summary>
        /// <param name="user">The entity doing the drag and drop.</param>
        /// <param name="dragged">The entity that is being dragged and dropped.</param>
        /// <param name="target">
        ///     The entity that <see cref="dragged"/> is being dropped onto.
        /// </param>
        public CanDropEventArgs(IEntity user, IEntity dragged, IEntity target)
        {
            User = user;
            Dragged = dragged;
            Target = target;
        }

        /// <summary>
        ///     The entity doing the drag and drop.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The entity that is being dragged and dropped.
        /// </summary>
        public IEntity Dragged { get; }

        /// <summary>
        ///     The entity that <see cref="Dragged"/> is being dropped onto.
        /// </summary>
        public IEntity Target { get; }
    }

    public class CanDragEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new instance of <see cref="CanDragEventArgs"/>.
        /// </summary>
        /// <param name="user">The entity doing the drag and drop.</param>
        /// <param name="dragged">The entity that is being dragged and dropped.</param>
        public CanDragEventArgs(IEntity user, IEntity dragged)
        {
            User = user;
            Dragged = dragged;
        }

        /// <summary>
        ///     The entity doing the drag and drop.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The entity that is being dragged and dropped.
        /// </summary>
        public IEntity Dragged { get; }
    }
}
