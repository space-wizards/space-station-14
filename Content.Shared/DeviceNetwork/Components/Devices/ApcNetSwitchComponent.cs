using Content.Shared.DeviceNetwork.Systems.Devices;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Devices;

/// <summary>
/// Stores the network-controlled on/off state of an APC network switch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ApcNetSwitchSystem))]
public sealed partial class ApcNetSwitchComponent : Component
{
    /// <summary>
    /// Whether the switch is on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool State;
}
