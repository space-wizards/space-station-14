
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    public class ApcNetSwitchComponent : Component
    {
        public override string Name => "ApcNetSwitch";

        [ViewVariables] public bool State;
    }
}
