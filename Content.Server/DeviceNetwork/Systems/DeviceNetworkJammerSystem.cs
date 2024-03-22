using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.DeviceNetwork.Systems;

public sealed class DeviceNetworkJammerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransformComponent, BeforePacketSentEvent>(BeforePacketSent);
    }

    private void BeforePacketSent(EntityUid uid, TransformComponent xform, BeforePacketSentEvent ev)
    {
        if (ev.Cancelled)
            return;

        var query = EntityQueryEnumerator<DeviceNetworkJammerComponent, TransformComponent>();

        while (query.MoveNext(out _, out var jammerComp, out var jammerXform))
        {
            if (!jammerComp.JammableNetworks.Contains(ev.NetworkId))
                continue;

            if ((jammerXform.Coordinates.TryDistance(EntityManager, ev.SenderTransform.Coordinates, out var distance) && distance <= jammerComp.Range)
                || (jammerXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var recvDistance) && recvDistance <= jammerComp.Range))
            {
                ev.Cancel();
                return;
            }
        }
    }

}
