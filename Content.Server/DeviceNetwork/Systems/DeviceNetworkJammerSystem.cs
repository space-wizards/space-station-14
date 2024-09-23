using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.DeviceNetwork.Systems;

/// <inheritdoc/>
public sealed class DeviceNetworkJammerSystem : SharedDeviceNetworkJammerSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransformComponent, BeforePacketSentEvent>(BeforePacketSent);
    }

    private void BeforePacketSent(Entity<TransformComponent> xform, ref BeforePacketSentEvent ev)
    {
        if (ev.Cancelled)
            return;

        var query = EntityQueryEnumerator<DeviceNetworkJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var jammerComp, out var jammerXform))
        {
            if (!_jammer.GetJammableNetworks((uid, jammerComp)).Contains(ev.NetworkId))
                continue;

            if (_transform.InRange(jammerXform.Coordinates, ev.SenderTransform.Coordinates, jammerComp.Range)
                || _transform.InRange(jammerXform.Coordinates, xform.Comp.Coordinates, jammerComp.Range))
            {
                ev.Cancel();
                return;
            }
        }
    }

}
