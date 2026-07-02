using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power.Systems;

namespace Content.Server.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkRequiresPowerSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceNetworkRequiresPowerComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    private void OnBeforePacketSent(EntityUid uid, DeviceNetworkRequiresPowerComponent component,
        BeforePacketSentEvent args)
    {
        if (!_power.IsPowered(uid))
        {
            args.Cancel();
        }
    }
}
