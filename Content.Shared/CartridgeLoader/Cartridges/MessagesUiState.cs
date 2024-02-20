using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiState : BoundUserInterfaceState
{
    public List<string> Contents;
    public MessagesUiStateMode Mode;

    public MessagesUiState(List<string> contents, MessagesUiStateMode mode)
    {
        Contents = contents;
        Mode = mode;
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
    public double Time;
}
