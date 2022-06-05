using Content.Server.DeviceNetwork.Systems.Devices;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    [Friend(typeof(ApcNetSwitchSystem))]
    public sealed class ApcNetSwitchComponent : Component
    {
        [ViewVariables] public bool State;
    }
}
