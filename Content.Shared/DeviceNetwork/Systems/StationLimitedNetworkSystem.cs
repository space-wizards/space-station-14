using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class StationLimitedNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceSystem = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private StationSystem _station = default!;

    [Dependency] private EntityQuery<StationTrackerComponent> _stationTrackerQuery;
    [Dependency] private EntityQuery<StationLimitedNetworkComponent> _stationLimitedQuery;

    [SubscribeLocalEvent]
    private void OnWiredInit(Entity<StationLimitedNetworkComponent> ent, ref MapInitEvent args)
    {
        _metaData.AddFlag(ent.Owner, MetaDataFlags.ExtraTransformEvents);
    }

    [SubscribeLocalEvent]
    private void OnGridChanged(Entity<StationLimitedNetworkComponent> ent, ref GridUidChangedEvent args)
    {
        if (MetaData(ent.Owner).EntityLifeStage < EntityLifeStage.MapInitialized
            || args.OldGrid == args.NewGrid)
            return;

        _deviceSystem.ReconnectDevice(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnConnectNetwork(Entity<StationNetworkManagerComponent> ent, ref DeviceAttemptConnectEvent args)
    {
        if (_deviceSystem.CheckNetworkIgnore(args, _stationLimitedQuery))
            return;

        args.Connected = _station.GetOwningStation(args.Entity.Owner) == ent.Comp.StationUid;
    }

    [SubscribeLocalEvent]
    private void OnManagerInit(Entity<StationNetworkManagerComponent> ent, ref DeviceNetworkManagerInitializeEvent args)
    {
        ent.Comp.StationUid = _station.GetOwningStation(args.Entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<StationLimitedNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (!_stationTrackerQuery.TryComp(ent, out var tracker)
            || !CheckStationId(args.Sender, ent.Comp.AllowNonStationPackets, tracker.Station))
            args.Cancelled = true;
    }

    /// <summary>
    /// Compares the station IDs of the sending and receiving network components.
    /// Returns false if either of them doesn't have a station ID or if their station ID isn't equal.
    /// Returns true even when the sending entity isn't tied to a station if `allowNonStationPackets` is set to true.
    /// </summary>
    private bool CheckStationId(Entity<StationLimitedNetworkComponent?, StationTrackerComponent?> sender, bool allowNonStationPackets, EntityUid? receiverStationId)
    {
        if (!receiverStationId.HasValue)
            return false;

        if (!_stationLimitedQuery.Resolve(sender.Owner, ref sender.Comp1, false))
            return allowNonStationPackets;

        if (!_stationTrackerQuery.Resolve(sender.Owner, ref sender.Comp2, false))
            return false;

        return sender.Comp2.Station == receiverStationId;
    }
}
