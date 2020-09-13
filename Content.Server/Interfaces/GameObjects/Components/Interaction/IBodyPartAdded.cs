using System;
using Content.Server.Body;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is added to their owning entity.
    /// </summary>
    public interface IBodyPartAdded
    {
        void BodyPartAdded(BodyPartAddedEventArgs eventArgs);
    }

    public class BodyPartAddedEventArgs : EventArgs
    {
        public BodyPartAddedEventArgs(ISharedBodyPart part, string slotName)
        {
            Part = part;
            SlotName = slotName;
        }

        public ISharedBodyPart Part { get; }

        public string SlotName { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is removed from their owning entity.
    /// </summary>
    public interface IBodyPartRemoved
    {
        void BodyPartRemoved(BodyPartRemovedEventArgs eventArgs);
    }

    public class BodyPartRemovedEventArgs : EventArgs
    {
        public BodyPartRemovedEventArgs(ISharedBodyPart part, string slotName)
        {
            Part = part;
            SlotName = slotName;
        }

        public ISharedBodyPart Part { get; }

        public string SlotName { get; }
    }
}
