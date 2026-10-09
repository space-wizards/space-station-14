using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Sends and receives device network messages wirelessly.
/// Devices sending and receiving need to be in range and on the same frequency.
/// If the range is not specified, will just check if the entity is on the same map.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WirelessNetworkComponent : Component
{
    /// <summary>
    /// Maximum distance at which wireless device network messages can be received.
    /// Null allows communication anywhere on the same map.
    /// </summary>
    [DataField]
    public float? Range;
}
