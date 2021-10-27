using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is added to their owning entity.
    /// </summary>
    public interface IBodyPartAdded : IComponent
    {
        /// <summary>
        ///     Called when a <see cref="SharedBodyPartComponent"/> is added to the
        ///     entity owning this component.
        /// </summary>
        /// <param name="args">Information about the part that was added.</param>
        void BodyPartAdded(BodyPartAddedEventArgs args);
    }


    [Serializable, NetSerializable]
    public class BodyPartAddedEventArgs : EventArgs
    {
        public BodyPartAddedEventArgs(string slot, SharedBodyPartComponent part)
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
        public SharedBodyPartComponent Part { get; }
    }
}
