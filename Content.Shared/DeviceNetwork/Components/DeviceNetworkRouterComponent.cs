using Content.Shared.DeviceNetwork.Payloads;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Re-routs <see cref="RoutedNetworkPayload"/>s to some other devices.
/// Useful for server setups where there are lots of payloads that have to be relayed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeviceNetworkRouterComponent : Component
{
    /// <summary>
    /// If not null, overrides the default transmit frequency of this router.
    /// </summary>
    [DataField]
    public uint? TransmitFrequency;
}
