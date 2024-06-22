using Robust.Shared.Serialization;

namespace Content.Shared.Chat.TypingIndicator;

[Serializable, NetSerializable]
public enum TypingIndicatorState
{
    None = 0,
    Idle = 1,
    Typing = 2,
}
