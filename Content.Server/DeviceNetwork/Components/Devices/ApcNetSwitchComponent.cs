using Content.Server.DeviceNetwork.Systems.Devices;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    [Access(typeof(ApcNetSwitchSystem))]
    public sealed partial class ApcNetSwitchComponent : Component
    {
        [ViewVariables] public bool State;
    }
}
