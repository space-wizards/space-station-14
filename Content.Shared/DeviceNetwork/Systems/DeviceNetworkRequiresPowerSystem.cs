using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed class DeviceNetworkRequiresPowerSystem : EntitySystem
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
}
