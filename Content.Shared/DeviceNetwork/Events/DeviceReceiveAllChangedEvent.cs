using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceNetworkComponent.ReceiveAll"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceReceiveAllChangedEvent(bool ReceiveAll);
