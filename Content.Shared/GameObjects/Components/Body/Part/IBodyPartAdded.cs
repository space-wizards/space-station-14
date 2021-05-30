#nullable enable
using System;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is added to their owning entity.
    /// </summary>
    public interface IBodyPartAdded : IComponent
    {
        /// <summary>
        ///     Called when a <see cref="IBodyPart"/> is added to the
        ///     entity owning this component.
        /// </summary>
        /// <param name="args">Information about the part that was added.</param>
        void BodyPartAdded(BodyPartAddedEventArgs args);
    }

    public class BodyPartAddedEventArgs : EventArgs
    {
        public BodyPartAddedEventArgs(string slot, IBodyPart part)
        {
            Slot = slot;
            Part = part;
        }

        /// <summary>
        ///     The slot that <see cref="Part"/> was added to.
        /// </summary>
        public string Slot { get; }

        /// <summary>
        ///     The part that was added.
        /// </summary>
        public IBodyPart Part { get; }
    }
}
