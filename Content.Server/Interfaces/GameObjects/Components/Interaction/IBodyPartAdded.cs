using System;
using Content.Server.Body;

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
        public BodyPartAddedEventArgs(IBodyPart part, string slotName)
        {
            Part = part;
            SlotName = slotName;
        }

        public IBodyPart Part { get; }

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
        public BodyPartRemovedEventArgs(IBodyPart part, string slotName)
        {
            Part = part;
            SlotName = slotName;
        }

        public IBodyPart Part { get; }

        public string SlotName { get; }
    }
}
