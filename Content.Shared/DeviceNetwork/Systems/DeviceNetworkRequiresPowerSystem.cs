using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkRequiresPowerSystem : BeforeDevicePayloadSystem<DeviceNetworkRequiresPowerComponent>
{
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceNetworkRequiresPowerComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    private void OnBeforePacketSent(Entity<DeviceNetworkRequiresPowerComponent> ent, ref BeforePacketSentEvent args)
    {
        if (!_power.IsPowered(ent.Owner))
        {
            args.Cancelled = true;
        }
    }

    protected override void OnBeforePayload(Entity<DeviceNetworkRequiresPowerComponent> ent, ref BeforePacketSentEvent args)
    {
        OnBeforePacketSent(ent, ref args);
    }
}
