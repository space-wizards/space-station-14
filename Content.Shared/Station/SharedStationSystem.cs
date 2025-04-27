using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Station;

public abstract partial class SharedStationSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<StationMemberComponent> _stationMemberQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _stationMemberQuery = GetEntityQuery<StationMemberComponent>();

        SubscribeLocalEvent<StationTrackerComponent, MapInitEvent>(OnTrackerMapInit);
        SubscribeLocalEvent<StationTrackerComponent, ComponentRemove>(OnTrackerRemove);
        SubscribeLocalEvent<StationTrackerComponent, EntGridChangedEvent>(OnTrackerGridChanged);
        SubscribeLocalEvent<StationTrackerComponent, MetaFlagRemoveAttemptEvent>(OnMetaFlagRemoveAttempt);
    }

    private void OnTrackerMapInit(Entity<StationTrackerComponent> ent, ref MapInitEvent args)
    {
        _meta.AddFlag(ent, MetaDataFlags.GridTracking);
        UpdateStationTracker(ent.AsNullable());
    }

    private void OnTrackerRemove(Entity<StationTrackerComponent> ent, ref ComponentRemove args)
    {
        _meta.RemoveFlag(ent, MetaDataFlags.GridTracking);
    }

    private void OnTrackerGridChanged(Entity<StationTrackerComponent> ent, ref EntGridChangedEvent args)
    {
        UpdateStationTracker((ent, ent.Comp, args.Transform));
    }

    private void OnMetaFlagRemoveAttempt(Entity<StationTrackerComponent> ent, ref MetaFlagRemoveAttemptEvent args)
    {
        if ((args.ToRemove & MetaDataFlags.GridTracking) != 0)
        {
            args.ToRemove &= ~MetaDataFlags.GridTracking;
        }
    }

    /// <summary>
    /// Updates the station tracker component based on entity's current location.
    /// </summary>
    [PublicAPI]
    public void UpdateStationTracker(Entity<StationTrackerComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1))
            return;

        var xform = ent.Comp2;

        if (!_xformQuery.Resolve(ent, ref xform))
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
    [PublicAPI]
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
    [PublicAPI]
    public EntityUid? GetCurrentStation(Entity<StationTrackerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return null;

        return ent.Comp.Station;
    }
}
