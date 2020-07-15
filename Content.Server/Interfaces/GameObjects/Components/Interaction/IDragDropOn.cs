using System;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    /// This interface allows the component's entity to be dragged and dropped onto by another entity and gives it
    /// behavior when that occurs.
    /// </summary>
    public interface IDragDropOn
    {
        /// <summary>
        /// Invoked server-side when another entity is being dragged and dropped onto this one
        ///
        /// There is no other server-side drag and drop check other than a range check, so make sure to validate
        /// if this object can be dropped on the dropped object!
        /// </summary>
        /// <returns>true iff an interaction occurred and no further interaction should
        /// be processed for this drop.</returns>
        bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
