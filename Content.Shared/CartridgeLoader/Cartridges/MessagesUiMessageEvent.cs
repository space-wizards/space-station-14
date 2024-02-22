using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiMessageEvent : CartridgeMessageEvent
{
    public readonly MessagesUiAction Action;
    public readonly string? Parameter;

    public MessagesUiMessageEvent(MessagesUiAction action, string? parameter)
    {
        Action = action;
        Parameter = parameter;
    }
}

[Serializable, NetSerializable]
public enum MessagesUiAction
{
    Send,
    ChangeChat
}


