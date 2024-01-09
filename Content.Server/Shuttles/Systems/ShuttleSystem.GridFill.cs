using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.Cargo.Components;
using Content.Shared.CCVar;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeGridFills()
    {
        SubscribeLocalEvent<GridSpawnComponent, StationPostInitEvent>(OnGridSpawnPostInit);
        SubscribeLocalEvent<GridFillComponent, MapInitEvent>(OnGridFillMapInit);

        _cfg.OnValueChanged(CCVars.GridFill, OnGridFillChange);
    }

    private void ShutdownGridFills()
    {
        _cfg.UnsubValueChanged(CCVars.GridFill, OnGridFillChange);
    }

    private void OnGridFillChange(bool obj)
    {
        // If you're doing this on live then god help you,
        if (obj)
        {
            var query = AllEntityQuery<GridSpawnComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                GridSpawns(uid, comp);
            }
        }
    }

    private void OnGridSpawnPostInit(EntityUid uid, GridSpawnComponent component, ref StationPostInitEvent args)
    {
        GridSpawns(uid, component);
    }

    private void GridSpawns(EntityUid uid, GridSpawnComponent component)
    {
        if (!_cfg.GetCVar(CCVars.GridFill))
            return;

        if (!TryComp<StationDataComponent>(uid, out var data))
        {
            return;
        }

        var targetGrid = _station.GetLargestGrid(data);

        if (targetGrid == null)
            return;

        // Spawn on a dummy map and try to FTL if possible, otherwise dump it.
        var mapId = _mapManager.CreateMap();
        var valid = true;
        var paths = new List<ResPath>();

        foreach (var group in component.Groups.Values)
        {
            if (group.Paths.Count == 0)
            {
                Log.Error($"Found no paths for GridSpawn");
                continue;
            }

            var count = _random.Next(group.MinCount, group.MaxCount);
            paths.Clear();

            for (var i = 0; i < count; i++)
            {
                // Round-robin so we try to avoid dupes where possible.
                if (paths.Count == 0)
                {
                    paths.AddRange(group.Paths);
                    _random.Shuffle(paths);
                }

                var path = paths[^1];
                paths.RemoveAt(paths.Count - 1);

                if (_loader.TryLoad(mapId, path.ToString(), out var ent) && ent.Count == 1)
                {
                    if (TryComp<ShuttleComponent>(ent[0], out var shuttle))
                    {
                        TryFTLProximity(ent[0], shuttle, targetGrid.Value);
                        _station.AddGridToStation(uid, ent[0]);
                    }
                    else
                    {
                        valid = false;
                    }
                }
                else
                {
                    valid = false;
                }

                if (!valid)
                {
                    Log.Error($"Error loading gridspawn for {ToPrettyString(uid)} / {path}");
                }
            }
        }

        _mapManager.DeleteMap(mapId);
    }

    private void OnGridFillMapInit(EntityUid uid, GridFillComponent component, MapInitEvent args)
    {
        if (!_cfg.GetCVar(CCVars.GridFill))
            return;

        if (!TryComp<DockingComponent>(uid, out var dock) ||
            !TryComp<TransformComponent>(uid, out var xform) ||
            xform.GridUid == null)
        {
            return;
        }

        // Spawn on a dummy map and try to dock if possible, otherwise dump it.
        var mapId = _mapManager.CreateMap();
        var valid = false;

        if (_loader.TryLoad(mapId, component.Path.ToString(), out var ent) &&
            ent.Count == 1 &&
            TryComp<TransformComponent>(ent[0], out var shuttleXform))
        {
            var escape = GetSingleDock(ent[0]);

            if (escape != null)
            {
                var config = _dockSystem.GetDockingConfig(ent[0], xform.GridUid.Value, escape.Value.Entity, escape.Value.Component, uid, dock);

                if (config != null)
                {
                    FTLDock(config, shuttleXform);

                    if (TryComp<StationMemberComponent>(xform.GridUid, out var stationMember))
                    {
                        _station.AddGridToStation(stationMember.Station, ent[0]);
                    }

                    valid = true;
                }
            }
        }

        if (!valid)
        {
            Log.Error($"Error loading gridfill dock for {ToPrettyString(uid)} / {component.Path}");
        }

        _mapManager.DeleteMap(mapId);
    }

    private (EntityUid Entity, DockingComponent Component)? GetSingleDock(EntityUid uid)
    {
        var dockQuery = GetEntityQuery<DockingComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);

        var rator = xform.ChildEnumerator;

        while (rator.MoveNext(out var child))
        {
            if (!dockQuery.TryGetComponent(child, out var dock))
                continue;

            return (child, dock);
        }

        return null;
    }
}
