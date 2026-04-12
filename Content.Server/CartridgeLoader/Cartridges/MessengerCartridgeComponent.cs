namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessengerCartridgeComponent : Component
{
    [DataField]
    public Dictionary<string, List<MessengerStoredMessage>> Messages = new();

    [DataField]
    public HashSet<string> UnreadContacts = new();

    [DataField]
    public string? ActiveChat;

    [DataField]
    public int MaxMessages = 50;

    [DataField]
    public int MaxMessageLength = 256;
}

public sealed class MessengerStoredMessage
{
    public string Sender;
    public string Content;
    public TimeSpan Timestamp;

    public MessengerStoredMessage(string sender, string content, TimeSpan timestamp)
    {
        Sender = sender;
        Content = content;
        Timestamp = timestamp;
    }
}