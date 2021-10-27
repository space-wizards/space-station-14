using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.HandLabeler
{
    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum HandLabelerUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents a <see cref="HandLabelerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class HandLabelerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentLabel { get; }

        public HandLabelerBoundUserInterfaceState(string currentLabel)
        {
            CurrentLabel = currentLabel;
        }
    }

    [Serializable, NetSerializable]
    public class HandLabelerLabelChangedMessage : BoundUserInterfaceMessage
    {
        public string Label { get; }

        public HandLabelerLabelChangedMessage(string label)
        {
            Label = label;
        }
    }
}
