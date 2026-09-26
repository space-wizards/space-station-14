using Content.Shared.DeviceNetwork.Systems.Devices;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Devices;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ApcNetSwitchSystem))]
public sealed partial class ApcNetSwitchComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool State;
}
