using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Station;
using Content.Shared.Station.Components;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// This system requires the StationLimitedNetworkComponent to be on the sending entity as well as the receiving entity
/// </summary>
public sealed partial class StationLimitedNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedStationSystem _stationSystem = default!;

    [Dependency] private EntityQuery<StationTrackerComponent> _stationTrackerQuery = default!;
    [Dependency] private EntityQuery<StationLimitedNetworkComponent> _stationLimitedQuery = default!;

    [SubscribeLocalEvent]
    private void OnManagerInitialize(Entity<StationNetworkManagerComponent> ent, ref DeviceNetworkManagerInitializeEvent args)
    {
        ent.Comp.StationId = _stationSystem.GetOwningStation(args.Entity);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<StationLimitedNetworkComponent> ent, ref GridUidChangedEvent args)
    {
        _deviceNetwork.ReconnectDevice(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnAttemptConnect(Entity<StationNetworkManagerComponent> ent, ref DeviceAttemptConnectEvent args)
    {
        if (_stationSystem.GetOwningStation(args.Entity) == ent.Comp.StationId)
            args.Connected = true;
    }

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<StationLimitedNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (_stationTrackerQuery.TryComp(ent, out var tracker)
            && !CheckStationId(args.Sender, ent.Comp.AllowNonStationPackets, tracker.Station))
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
