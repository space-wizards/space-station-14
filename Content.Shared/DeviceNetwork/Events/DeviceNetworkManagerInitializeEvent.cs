using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised on a <see cref="DeviceNetworkManagerComponent"/> entity when it initializes.
/// </summary>
[ByRefEvent]
public readonly record struct DeviceNetworkManagerInitializeEvent(Entity<DeviceNetworkComponent> Entity);
