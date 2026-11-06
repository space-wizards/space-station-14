using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WirelessNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    [Dependency] private EntityQuery<WirelessNetworkComponent> _wirelessQuery;

    /// <summary>
    /// Reconnects a wireless device when its map changes.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapChanged(Entity<WirelessNetworkComponent> ent, ref EntParentChangedMessage args)
    {
        if (MetaData(ent.Owner).EntityLifeStage < EntityLifeStage.MapInitialized
            || args.OldMapId == args.Transform.MapUid)
            return;

        _deviceSystem.ReconnectDevice(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnConnectNetwork(Entity<WirelessNetworkManagerComponent> ent, ref DeviceAttemptConnectEvent args)
    {
        if (_deviceSystem.CheckNetworkIgnore(args, _wirelessQuery))
            return;

        args.Connected = Transform(args.Entity.Owner).MapUid == ent.Comp.MapUid;
    }

    [SubscribeLocalEvent]
    private void OnManagerInit(Entity<WirelessNetworkManagerComponent> ent, ref DeviceNetworkManagerInitializeEvent args)
    {
        ent.Comp.MapUid = Transform(args.Entity.Owner).MapUid;
    }

    /// <summary>
    /// Checks whether a device is in range.
    /// </summary>
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
