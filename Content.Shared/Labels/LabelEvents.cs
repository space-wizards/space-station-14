using Robust.Shared.Serialization;

namespace Content.Shared.Labels
{
    /// <summary>
    /// Key representing which <see cref="PlayerBoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum HandLabelerUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public enum PaperLabelVisuals
    {
        HasLabel,
    }

    /// <summary>
    /// Represents a <see cref="HandLabelerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class HandLabelerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentLabel { get; }

        public HandLabelerBoundUserInterfaceState(string currentLabel)
        {
            CurrentLabel = currentLabel;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HandLabelerLabelChangedMessage : BoundUserInterfaceMessage
    {
        public string Label { get; }

        public HandLabelerLabelChangedMessage(string label)
        {
            Label = label;
        }
    }
}
