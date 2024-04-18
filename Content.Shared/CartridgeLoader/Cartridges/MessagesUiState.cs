using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiState(MessagesUiStateMode mode, List<(string, int?)> contents, string? name = null) : BoundUserInterfaceState
{
    public List<(string, int?)>? Contents = contents;
    public MessagesUiStateMode Mode = mode;
    public string? Name = name;
}

[Serializable, NetSerializable]
public enum MessagesUiStateMode : byte
{
    UserList,
    Chat
}

[Serializable, NetSerializable]
public partial struct MessagesMessageData : byte
{
    public int SenderId;
    public int ReceiverId;
    public string Content;
    public TimeSpan Time;
}

[Serializable, NetSerializable]
public enum MessagesKeys : byte
{
    Nanotrasen,
    Syndicate
}
