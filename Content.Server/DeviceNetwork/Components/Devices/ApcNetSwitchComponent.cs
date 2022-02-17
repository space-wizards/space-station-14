using Content.Server.DeviceNetwork.Systems.Devices;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    [Friend(typeof(ApcNetSwitchSystem))]
    public sealed class ApcNetSwitchComponent : Component
    {
        [ViewVariables] public bool State;
    }
}
