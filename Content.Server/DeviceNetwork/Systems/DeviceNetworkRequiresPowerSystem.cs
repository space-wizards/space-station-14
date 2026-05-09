using Content.Server.DeviceNetwork.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Server.DeviceNetwork.Systems;

public sealed class DeviceNetworkRequiresPowerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceNetworkRequiresPowerComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    private void OnBeforePacketSent(EntityUid uid, DeviceNetworkRequiresPowerComponent component,
        BeforePacketSentEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
        {
            args.Cancel();
        }
    }
}
