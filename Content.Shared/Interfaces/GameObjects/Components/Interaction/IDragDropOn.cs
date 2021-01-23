using Robust.Shared;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface allows the component's entity to be dragged and dropped
    ///     onto by another entity and gives it behavior when that occurs.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IDragDropOn
    {
        /// <summary>
        ///     Invoked when another entity is being dragged and dropped
        ///     onto this one before invoking <see cref="DragDropOn"/>.
        ///     Note that other drag and drop interactions may be attempted if
        ///     this one fails.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>true if <see cref="eventArgs"/> is valid, false otherwise.</returns>
        bool CanDragDropOn(DragDropEventArgs eventArgs);

        /// <summary>
        ///     Invoked server-side when another entity is being dragged and dropped
        ///     onto this one before invoking <see cref="DragDropOn"/>
        ///     Note that other drag and drop interactions may be attempted if
        ///     this one fails.
        /// </summary>
        /// <returns>
        ///     true if an interaction occurred and no further interaction should
        ///     be processed for this drop.
        /// </returns>
        bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
