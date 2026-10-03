using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Station.Components;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class StationLimitedNetworkSystem : EntitySystem
{
    [Dependency] private EntityQuery<StationTrackerComponent> _stationTrackerQuery = default!;
    [Dependency] private EntityQuery<StationLimitedNetworkComponent> _stationLimitedQuery = default!;

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
