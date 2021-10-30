using Content.Server.DeviceNetwork.Systems.Devices;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    [Friend(typeof(ApcNetSwitchSystem))]
    public class ApcNetSwitchComponent : Component
    {
        public override string Name => "ApcNetSwitch";

        [ViewVariables] public bool State;
    }
}
