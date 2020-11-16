using System;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is removed from their owning entity.
    /// </summary>
    public interface IBodyPartRemoved
    {
        /// <summary>
        ///     Called when a <see cref="IBodyPart"/> is removed from the
        ///     entity owning this component.
        /// </summary>
        /// <param name="args">Information about the part that was removed.</param>
        void BodyPartRemoved(BodyPartRemovedEventArgs args);
    }

    public class BodyPartRemovedEventArgs : EventArgs
    {
        public BodyPartRemovedEventArgs(IBodyPart part, string slot)
        {
            Part = part;
            Slot = slot;
        }

        /// <summary>
        ///     The part that was removed.
        /// </summary>
        public IBodyPart Part { get; }

        /// <summary>
        ///     The slot that <see cref="Part"/> was removed from.
        /// </summary>
        public string Slot { get; }
    }
}
