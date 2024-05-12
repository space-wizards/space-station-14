using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiMessageEvent : CartridgeMessageEvent
{
    public readonly MessagesUiAction Action;
    public readonly int? TargetChatUid;
    public readonly string? StringInput;

    public MessagesUiMessageEvent(MessagesUiAction action, string? stringInput, int? targetChatUid)
    {
        Action = action;
        TargetChatUid = targetChatUid;
        StringInput = stringInput;
    }
}

[Serializable, NetSerializable]
public enum MessagesUiAction : byte
{
    Send,
    ChangeChat
}


