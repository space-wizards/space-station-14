using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
///     Component that indicates that this device networked entity requires power
///     in order to receive a packet. Having this component will cancel all packet events
///     if the entity is not powered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeviceNetworkRequiresPowerComponent : Component;
