// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Messenger;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState
{
    public MessengerContact ClientContact = new();
    public Dictionary<uint, MessengerChatUiState> Chats = new();
    public Dictionary<uint, MessengerMessage> Messages = new();
    public Dictionary<uint, MessengerContact> Contacts = new();
}

[Serializable, NetSerializable]
public sealed class MessengerChatUiState
{
    public uint Id;
    public string Name;
    public MessengerChatKind Kind;
    public HashSet<uint> Members;
    public HashSet<uint> Messages;
    public uint? LastMessage;
    public int SortNumber;
    public bool NewMessages;
    public bool ForceUpdate;

    public MessengerChatUiState(uint id, string? name, MessengerChatKind kind, HashSet<uint> members,
        HashSet<uint> messages, uint? lastMessage, int sortNumber)
    {
        Id = id;
        Name = name ?? "unknown";
        Kind = kind;
        Members = members;
        Messages = messages;
        LastMessage = lastMessage;
        SortNumber = sortNumber;
        NewMessages = true;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerClientContactUiState : BoundUserInterfaceState
{
    public MessengerContact ClientContact;

    public MessengerClientContactUiState(MessengerContact clientContact)
    {
        ClientContact = clientContact;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerContactUiState : BoundUserInterfaceState
{
    public List<MessengerContact> Contacts;

    public MessengerContactUiState(List<MessengerContact> contacts)
    {
        Contacts = contacts;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerMessagesUiState : BoundUserInterfaceState
{
    // uint - chatId
    public List<MessengerMessage> Messages;

    public MessengerMessagesUiState(List<MessengerMessage> messages)
    {
        Messages = messages;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerChatUpdateUiState : BoundUserInterfaceState
{
    public List<MessengerChat> Chats;

    public MessengerChatUpdateUiState(List<MessengerChat> chats)
    {
        Chats = chats;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerErrorUiState : BoundUserInterfaceState
{
    public string Text;

    public MessengerErrorUiState(string text)
    {
        Text = text;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerNewChatMessageUiState : BoundUserInterfaceState
{
    public uint ChatId;
    public MessengerMessage Message;

    public MessengerNewChatMessageUiState(uint chatId, MessengerMessage message)
    {
        ChatId = chatId;
        Message = message;
    }
}
