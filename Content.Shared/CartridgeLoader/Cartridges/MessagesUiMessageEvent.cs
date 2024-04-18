using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiMessageEvent : CartridgeMessageEvent
{
    public readonly MessagesUiAction Action;
    public readonly int? UidInput;
    public readonly string? StringInput;

    public MessagesUiMessageEvent(MessagesUiAction action, string? stringInput, int? uidInput)
    {
        Action = action;
        UidInput = uidInput;
        StringInput = stringInput;
    }
}

[Serializable, NetSerializable]
public enum MessagesUiAction : byte
{
    Send,
    ChangeChat
}


