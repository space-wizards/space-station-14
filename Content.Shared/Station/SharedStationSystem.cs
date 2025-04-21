using Content.Shared.Station.Components;
using Robust.Shared.Map;

namespace Content.Shared.Station;

public abstract class SharedStationSystem : EntitySystem
{
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<StationMemberComponent> _stationMemberQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _stationMemberQuery = GetEntityQuery<StationMemberComponent>();

        SubscribeLocalEvent<StationTrackerComponent, ComponentStartup>(OnStationTrackerStartup);
        SubscribeLocalEvent<StationTrackerComponent, MoveEvent>(OnStationTrackerMoved);
    }

    private void OnStationTrackerStartup(Entity<StationTrackerComponent> ent, ref ComponentStartup args)
    {
        UpdateStationTracker(ent.AsNullable());
    }

    private void OnStationTrackerMoved(Entity<StationTrackerComponent> ent, ref MoveEvent args)
    {
        UpdateStationTracker(ent.AsNullable());
    }

    /// <summary>
    /// Updates the station tracker component based on entity's current location.
    /// </summary>
    public void UpdateStationTracker(Entity<StationTrackerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_xformQuery.TryGetComponent(ent, out var xform))
            return;

        // Entity is in nullspace or not on a grid
        if (xform.MapID == MapId.Nullspace || xform.GridUid == null)
        {
            SetStation(ent, null);
            return;
        }

        // Check if the grid is part of a station
        if (!_stationMemberQuery.TryGetComponent(xform.GridUid.Value, out var stationMember))
        {
            SetStation(ent, null);
            return;
        }

        SetStation(ent, stationMember.Station);
    }

    /// <summary>
    /// Sets the station for a StationTrackerComponent.
    /// </summary>
    public void SetStation(Entity<StationTrackerComponent?> ent, EntityUid? station)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Station == station)
            return;

        ent.Comp.Station = station;
        Dirty(ent);
    }

    /// <summary>
    /// Gets the station an entity is currently on, if any.
    /// </summary>
    public EntityUid? GetCurrentStation(Entity<StationTrackerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return null;

        return ent.Comp.Station;
    }
}
