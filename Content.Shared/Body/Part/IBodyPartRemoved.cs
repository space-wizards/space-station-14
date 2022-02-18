using System;
using Content.Shared.Body.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is removed from their owning entity.
    /// </summary>
    public interface IBodyPartRemoved
    {
        /// <summary>
        ///     Called when a <see cref="SharedBodyPartComponent"/> is removed from the
        ///     entity owning this component.
        /// </summary>
        /// <param name="args">Information about the part that was removed.</param>
        void BodyPartRemoved(BodyPartRemovedEventArgs args);
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartRemovedEventArgs : EventArgs
    {
        public BodyPartRemovedEventArgs(string slot, SharedBodyPartComponent part)
        {
            Slot = slot;
            Part = part;
        }

        /// <summary>
        ///     The slot that <see cref="Part"/> was removed from.
        /// </summary>
        public string Slot { get; }

        /// <summary>
        ///     The part that was removed.
        /// </summary>
        public SharedBodyPartComponent Part { get; }
    }
}
