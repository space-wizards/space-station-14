using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components;
using Robust.Server.GameObjects;

namespace Content.Server.DeviceNetwork.Systems;

public sealed class DeviceNetworkJammerSystem : EntitySystem
{
    [Dependency] private TransformSystem _transform = default!;
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

            if (jammerXform.Coordinates.InRange(EntityManager, _transform, ev.SenderTransform.Coordinates, jammerComp.Range)
                || jammerXform.Coordinates.InRange(EntityManager, _transform, xform.Coordinates, jammerComp.Range))
            {
                ev.Cancel();
                return;
            }
        }
    }

}
