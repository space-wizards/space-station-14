using Robust.Shared.Serialization;

namespace Content.Shared.Fax;

[Serializable, NetSerializable]
public enum FaxUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class FaxUiState : BoundUserInterfaceState
{
    public string DeviceName { get; }
    public Dictionary<string, string> AvailablePeers { get; }
    public string? DestinationAddress { get; }
    public bool IsPaperInserted { get; }
    public bool CanSend { get; }

    public FaxUiState(string deviceName,
        Dictionary<string, string> peers,
        bool canSend,
        bool isPaperInserted,
        string? destAddress)
    {
        DeviceName = deviceName;
        AvailablePeers = peers;
        IsPaperInserted = isPaperInserted;
        CanSend = canSend;
        DestinationAddress = destAddress;
    }
}

[Serializable, NetSerializable]
public sealed class FaxSendMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class FaxRefreshMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class FaxDestinationMessage : BoundUserInterfaceMessage
{
    public string Address { get; }

    public FaxDestinationMessage(string address)
    {
        Address = address;
    }
}
