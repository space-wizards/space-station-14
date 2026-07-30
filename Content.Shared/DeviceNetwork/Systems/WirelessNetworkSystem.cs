using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WirelessNetworkSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    [Dependency] private EntityQuery<WirelessNetworkComponent> _wirelessQuery = default!;

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<WirelessNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        var ownPosition = args.SenderPosition;
        var xform = Transform(ent);

        // not a wireless to wireless connection, just let it happen
        if (!_wirelessQuery.TryComp(args.Sender, out var sendingComponent))
            return;

        if (xform.MapID != args.SenderTransform.MapID
            || (ownPosition - _transformSystem.GetWorldPosition(xform)).Length() > sendingComponent.Range)
        {
            args.Cancelled = true;
        }
    }
}
