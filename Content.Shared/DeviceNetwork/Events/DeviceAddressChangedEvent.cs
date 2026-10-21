using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceNetworkComponent.Address"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceAddressChangedEvent(DeviceAddress OldAddress, DeviceAddress NewAddress);
