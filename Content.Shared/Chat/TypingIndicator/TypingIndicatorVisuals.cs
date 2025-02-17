using Robust.Shared.Serialization;

namespace Content.Shared.Chat.TypingIndicator;

[Serializable, NetSerializable]
public enum TypingIndicatorVisuals : byte
{
    State, // Corvax-TypingIndicator
}

[Serializable]
public enum TypingIndicatorLayers : byte
{
    Base
}
