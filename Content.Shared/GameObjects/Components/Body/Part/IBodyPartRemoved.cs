using System;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is removed from their owning entity.
    /// </summary>
    public interface IBodyPartRemoved
    {
        void BodyPartRemoved(BodyPartRemovedEventArgs args);
    }

    public class BodyPartRemovedEventArgs : EventArgs
    {
        public BodyPartRemovedEventArgs(IBodyPart part, string slot)
        {
            Part = part;
            Slot = slot;
        }

        public IBodyPart Part { get; }

        public string Slot { get; }
    }
}
