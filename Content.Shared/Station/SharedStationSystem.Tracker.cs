using Content.Shared.Examine;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Station;

public abstract partial class SharedStationSystem
{
    [SubscribeLocalEvent]
    private void OnTrackerMapInit(Entity<StationTrackerComponent> ent, ref MapInitEvent args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
        UpdateStationTracker(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnTrackerGridChanged(Entity<StationTrackerComponent> ent, ref GridUidChangedEvent args)
    {
        UpdateStationTracker((ent, ent.Comp, args.Transform));
    }

    [SubscribeLocalEvent]
    private void OnMetaFlagRemoveAttempt(Entity<StationTrackerComponent> ent, ref MetaFlagRemoveAttemptEvent args)
    {
        if ((args.ToRemove & MetaDataFlags.ExtraTransformEvents) != 0 &&
            ent.Comp.LifeStage <= ComponentLifeStage.Running)
        {
            args.ToRemove &= ~MetaDataFlags.ExtraTransformEvents;
        }
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<StationTrackerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Station != null && ent.Comp.Examinable)
            args.PushMarkup(Loc.GetString("station-tracker-component-examine", ("stationName", Name(ent.Comp.Station.Value))));
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

        if (!Resolve(ent, ref xform))
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
}
