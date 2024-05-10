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
    public bool CanCopy { get; }

    public FaxUiState(string deviceName,
        Dictionary<string, string> peers,
        bool canSend,
        bool canCopy,
        bool isPaperInserted,
        string? destAddress)
    {
        DeviceName = deviceName;
        AvailablePeers = peers;
        IsPaperInserted = isPaperInserted;
        CanSend = canSend;
        CanCopy = canCopy;
        DestinationAddress = destAddress;
    }
}

[Serializable, NetSerializable]
public sealed class FaxFileMessage : BoundUserInterfaceMessage
{
    public string Content;
    public bool OfficePaper;

    public FaxFileMessage(string content, bool officePaper)
    {
        Content = content;
        OfficePaper = officePaper;
    }
}

public static class FaxFileMessageValidation
{
    public const int MaxContentSize = 10000;
}

[Serializable, NetSerializable]
public sealed class FaxCopyMessage : BoundUserInterfaceMessage
{
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
