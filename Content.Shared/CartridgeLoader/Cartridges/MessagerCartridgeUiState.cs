using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagerCartridgeUiState : BoundUserInterfaceState
{
    public MessagerStatus Status;
    public Dictionary<int, MessagerUserEntry> Users;

    public MessagerCartridgeUiState(MessagerStatus status, Dictionary<int, MessagerUserEntry> users)
    {
        Status = status;
        Users = users;
    }
}

/// <summary>
/// User list client
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class MessagerUserEntry
{
    public int Id;
    public string Name;

    public MessagerUserEntry(int id, string name)
    {
        Id = id;
        Name = name;
    }
}


/// <summary>
/// Server connection status
/// </summary>
[Serializable, NetSerializable]
public enum MessagerStatus : byte
{
    Connecting,
    Connected,
    ConnectionLost
};
