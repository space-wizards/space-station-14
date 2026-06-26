using Content.Server.DeviceNetwork.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.DeviceNetwork.Systems;

public sealed class DeviceNetworkRequiresPowerSystem : BeforeDevicePayloadSystem<DeviceNetworkRequiresPowerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceNetworkRequiresPowerComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    private void OnBeforePacketSent(Entity<DeviceNetworkRequiresPowerComponent> ent, ref BeforePacketSentEvent args)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            args.Cancelled = true;
        }
    }

    protected override void OnBeforePayload(Entity<DeviceNetworkRequiresPowerComponent> ent, ref BeforePacketSentEvent args)
    {
        OnBeforePacketSent(ent, ref args);
    }
}
