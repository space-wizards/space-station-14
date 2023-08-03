// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.Messenger;

[Serializable, NetSerializable]
public sealed class MessengerChat
{
    public uint Id;
    public string? Name;
    public HashSet<uint> MembersId = new();
    public HashSet<uint> MessagesId = new();
    public MessengerChatKind Kind;
    public uint? LastMessageId;

    public MessengerChat(string? name, MessengerChatKind kind, HashSet<uint> membersId)
    {
        Name = name ?? "unknown";
        MembersId = membersId;
        Kind = kind;
    }

    public MessengerChat()
    {
        Name = "";
        Kind = MessengerChatKind.Contact;
        MembersId = new();
    }
}

[Serializable, NetSerializable]
public enum MessengerChatKind
{
    Contact,
    Chat,
    Channel,
    Bot,
}

[Serializable, NetSerializable]
public sealed class MessengerContact
{
    public uint Id;
    public string? Name;
    public HashSet<string> ActiveAddresses = new();

    public MessengerContact(string name, string netAddress)
    {
        Name = name;
        ActiveAddresses.Add(netAddress);
    }

    public MessengerContact()
    {
        Name = "";
    }
}

[Serializable, NetSerializable]
public sealed class MessengerMessage
{
    public uint Id;
    public uint ChatId;
    public uint FromContactId;
    public TimeSpan Time;
    public string Text;

    public MessengerMessage(uint chatId, uint fromContactId, TimeSpan time, string text)
    {
        ChatId = chatId;
        FromContactId = fromContactId;
        Time = time;
        Text = text;
    }

    public MessengerMessage()
    {
        FromContactId = 0;
        Time = new TimeSpan();
        Text = "";
    }
}
