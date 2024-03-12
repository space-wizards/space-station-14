using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiState : BoundUserInterfaceState
{
    public List<(string,string)>? Contents;
    public MessagesUiStateMode Mode;
    public string? Name;

    public MessagesUiState(MessagesUiStateMode mode, List<(string,string)>? contents = null, string? name = null)
    {
        Contents = contents;
        Mode = mode;
        Name = name;
    }
}

[Serializable, NetSerializable]
public enum MessagesUiStateMode
{
    UserList,
    Chat
}

[Serializable, NetSerializable]
public partial struct MessagesMessageData
{
    public string SenderId;
    public string ReceiverId;
    public string Content;
    public TimeSpan Time;
}

[Serializable, NetSerializable]
public enum MessagesKeys
{
    Nanotrasen,
    Syndicate
}
