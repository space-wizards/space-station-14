using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

///<summary>
/// The state of the messages app interface.
/// Mode switches whether the UI should display a list of other users or a particular chat.
/// Contents contains either the names of users and their ids in the messages system or simply a list of message strings.
///</summary>
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

///<summary>
/// Data of a single message in the system, containing the ids of the sender and recipient, the text content and the time it was sent.
///</summary>
[Serializable, NetSerializable]
public partial struct MessagesMessageData
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
