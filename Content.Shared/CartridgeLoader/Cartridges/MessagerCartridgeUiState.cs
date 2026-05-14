using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagerCartridgeUiState : BoundUserInterfaceState
{
    public MessagerStatus Status;

    public MessagerCartridgeUiState(MessagerStatus status)
    {
        Status = status;
    }
}


/// <summary>
///     Server connection status
/// </summary>
[Serializable, NetSerializable]
public enum MessagerStatus : byte
{
    Connecting,
    Connected,
    ConnectionLost
};
