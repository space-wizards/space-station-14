using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
///     Component that indicates that this device networked entity requires power
///     in order to receive or send a packet. Having this component will
///     disconnect and reconnect the device from its network depending on the power state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeviceNetworkRequiresPowerComponent : Component;
