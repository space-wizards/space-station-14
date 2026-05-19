using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class MessengerServerSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}

