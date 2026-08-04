using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WirelessNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    [Dependency] private EntityQuery<WirelessNetworkComponent> _wirelessQuery = default!;

    [SubscribeLocalEvent]
    private void OnManagerInitialize(Entity<WirelessNetworkManagerComponent> ent, ref DeviceNetworkManagerInitializeEvent args)
    {
        ent.Comp.MapId = Transform(args.Entity).MapUid;
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<WirelessNetworkComponent> ent, ref EntParentChangedMessage args)
    {
        if (args.OldMapId == args.Transform.MapUid)
            return;

        _deviceNetwork.ReconnectDevice(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnAttemptConnect(Entity<WirelessNetworkManagerComponent> ent, ref DeviceAttemptConnectEvent args)
    {
        if (Transform(args.Entity).MapUid == ent.Comp.MapId)
            args.Connected = true;
    }

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<WirelessNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        var ownPosition = args.SenderPosition;
        var xform = Transform(ent);

        // not a wireless to wireless connection, just let it happen
        if (!_wirelessQuery.TryComp(args.Sender, out var sendingComponent))
            return;

        if (xform.MapID != args.SenderTransform.MapID
            || sendingComponent.Range != null
            && (ownPosition - _transformSystem.GetWorldPosition(xform)).Length() > sendingComponent.Range)
        {
            args.Cancelled = true;
        }
    }
}
