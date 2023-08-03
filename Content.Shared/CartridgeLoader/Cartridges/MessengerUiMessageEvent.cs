// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerSendMessageUiEvent : CartridgeMessageEvent
{
    public readonly uint ChatId;
    public readonly string MessageText;

    public MessengerSendMessageUiEvent(uint chatId, string messageText)
    {
        ChatId = chatId;
        MessageText = messageText;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerUpdateStateUiEvent : CartridgeMessageEvent
{
    public readonly HashSet<uint>? CurrentContacts;
    public readonly HashSet<uint>? CurrentMessages;
    public readonly HashSet<uint>? CurrentChats;

    public readonly bool IsFullState;

    public MessengerUpdateStateUiEvent(bool isFullState)
    {
        IsFullState = isFullState;
    }

    public MessengerUpdateStateUiEvent(HashSet<uint>? currentContacts, HashSet<uint>? currentMessages,
        HashSet<uint>? currentChats)
    {
        if (currentContacts is { Count: > 0 })
            CurrentContacts = currentContacts;

        if (currentMessages is { Count: > 0 })
            CurrentMessages = currentMessages;

        if (currentChats is { Count: > 0 })
            CurrentChats = currentChats;

        IsFullState = false;
    }
}
