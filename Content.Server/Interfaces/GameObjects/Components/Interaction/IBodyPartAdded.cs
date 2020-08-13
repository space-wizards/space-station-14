using System;
using Content.Server.Health.BodySystem.BodyPart;

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
        public BodyPartAddedEventArgs(BodyPart part, string slotName)
        {
            Part = part;
            SlotName = slotName;
        }

        public BodyPart Part { get; }

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
        public BodyPartRemovedEventArgs(BodyPart part, string slotName)
        {
            Part = part;
            SlotName = slotName;
        }

        public BodyPart Part { get; }

        public string SlotName { get; }
    }
}
