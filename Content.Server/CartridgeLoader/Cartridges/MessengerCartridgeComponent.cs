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

[DataDefinition]
public sealed partial class MessengerStoredMessage
{
    [DataField]
    public string Sender = string.Empty;

    [DataField]
    public string Content = string.Empty;

    [DataField]
    public TimeSpan Timestamp;

    public MessengerStoredMessage() { }

    public MessengerStoredMessage(string sender, string content, TimeSpan timestamp)
    {
        Sender = sender;
        Content = content;
        Timestamp = timestamp;
    }
}
