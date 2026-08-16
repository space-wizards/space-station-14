namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised when a device is connected to a network
/// </summary>
public sealed class DeviceNetworkConnectedEvent : EntityEventArgs
{
}

/// <summary>
/// Event raised when a device is disconnected from a network
/// </summary>
public sealed class DeviceNetworkDisconnectedEvent : EntityEventArgs
{
}
