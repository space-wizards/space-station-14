using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised on a manager entity to determine if a specific entity is allowed to connect to this network.
/// </summary>
[ByRefEvent]
public record struct DeviceAttemptConnectEvent(Entity<DeviceNetworkComponent> Entity, DeviceNetworkManagerComponent ManagerComp, bool Connected = false);
