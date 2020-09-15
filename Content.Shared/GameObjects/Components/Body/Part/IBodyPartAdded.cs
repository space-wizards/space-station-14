using System;

namespace Content.Shared.GameObjects.Components.Body.Part
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
}
