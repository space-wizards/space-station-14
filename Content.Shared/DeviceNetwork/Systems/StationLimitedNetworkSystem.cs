using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Station;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// This system requires the StationLimitedNetworkComponent to be on the the sending entity as well as the receiving entity
/// </summary>
public sealed partial class StationLimitedNetworkSystem : BeforeDevicePayloadSystem<StationLimitedNetworkComponent>
{
    [Dependency] private SharedStationSystem _stationSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationLimitedNetworkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationLimitedNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    /// <summary>
    /// Sets the station id the device is limited to.
    /// </summary>
    public void SetStation(Entity<StationLimitedNetworkComponent?> ent, EntityUid? stationId)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.StationId = stationId;
    }

    /// <summary>
    /// Tries to set the station id to the current station if the device is currently on a station
    /// </summary>
    public bool TrySetStationId(Entity<StationLimitedNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp) || !Transform(ent).GridUid.HasValue)
            return false;

        ent.Comp.StationId = _stationSystem.GetOwningStation(ent);
        return ent.Comp.StationId.HasValue;
    }

    /// <summary>
    /// Set the station id to the one the entity is on when the station limited component is added
    /// </summary>
    private void OnMapInit(Entity<StationLimitedNetworkComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StationId = _stationSystem.GetOwningStation(ent);
    }

    /// <summary>
    /// Checks if both devices are limited to the same station
    /// </summary>
    private void OnBeforePacketSent(Entity<StationLimitedNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (!ent.Comp.StationId.HasValue)
            TrySetStationId(ent.AsNullable());

        if (!CheckStationId(args.Sender, ent.Comp.AllowNonStationPackets, ent.Comp.StationId))
        {
            args.Cancelled = true;
        }
    }

    protected override void OnBeforePayload(Entity<StationLimitedNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        OnBeforePacketSent(ent, ref args);
    }

    /// <summary>
    /// Compares the station IDs of the sending and receiving network components.
    /// Returns false if either of them doesn't have a station ID or if their station ID isn't equal.
    /// Returns true even when the sending entity isn't tied to a station if `allowNonStationPackets` is set to true.
    /// </summary>
    private bool CheckStationId(Entity<StationLimitedNetworkComponent?> sender, bool allowNonStationPackets, EntityUid? receiverStationId)
    {
        if (!receiverStationId.HasValue)
            return false;

        if (!Resolve(sender.Owner, ref sender.Comp, false))
            return allowNonStationPackets;

        if (!sender.Comp.StationId.HasValue)
            TrySetStationId(sender);

        return sender.Comp.StationId == receiverStationId;
    }
}
