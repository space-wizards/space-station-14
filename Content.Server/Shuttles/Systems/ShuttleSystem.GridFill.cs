using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeGridFills()
    {
        SubscribeLocalEvent<GridSpawnComponent, StationPostInitEvent>(OnGridSpawnPostInit);
        SubscribeLocalEvent<StationCargoShuttleComponent, StationPostInitEvent>(OnCargoSpawnPostInit);

        SubscribeLocalEvent<GridFillComponent, MapInitEvent>(OnGridFillMapInit);

        Subs.CVar(_cfg, CCVars.GridFill, OnGridFillChange);
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

            var cargoQuery = AllEntityQuery<StationCargoShuttleComponent>();

            while (cargoQuery.MoveNext(out var uid, out var comp))
            {
                CargoSpawn(uid, comp);
            }
        }
    }

    private void OnGridSpawnPostInit(EntityUid uid, GridSpawnComponent component, ref StationPostInitEvent args)
    {
        GridSpawns(uid, component);
    }

    private void OnCargoSpawnPostInit(EntityUid uid, StationCargoShuttleComponent component, ref StationPostInitEvent args)
    {
        CargoSpawn(uid, component);
    }

    private void CargoSpawn(EntityUid uid, StationCargoShuttleComponent component)
    {
        if (!_cfg.GetCVar(CCVars.GridFill))
            return;

        if (!TryComp(uid, out StationDataComponent? dataComp))
            return;

        var targetGrid = _station.GetLargestGrid(dataComp);

        if (targetGrid == null)
            return;

        _mapSystem.CreateMap(out var mapId);

        if (_loader.TryLoadGrid(mapId, component.Path, out var ent))
        {
            if (HasComp<ShuttleComponent>(ent))
                TryFTLProximity(ent.Value, targetGrid.Value);

            _station.AddGridToStation(uid, ent.Value);
        }

        _mapSystem.DeleteMap(mapId);
    }

    private bool TryDungeonSpawn(Entity<MapGridComponent?> targetGrid, DungeonSpawnGroup group, out EntityUid spawned)
    {
        spawned = EntityUid.Invalid;

        if (!_gridQuery.Resolve(targetGrid.Owner, ref targetGrid.Comp))
        {
            return false;
        }

        var dungeonProtoId = _random.Pick(group.Protos);

        if (!_protoManager.TryIndex(dungeonProtoId, out var dungeonProto))
        {
            return false;
        }

        var targetPhysics = _physicsQuery.Comp(targetGrid);
        var spawnCoords = new EntityCoordinates(targetGrid, targetPhysics.LocalCenter);

        if (group.MinimumDistance > 0f)
        {
            var distancePadding = MathF.Max(targetGrid.Comp.LocalAABB.Width, targetGrid.Comp.LocalAABB.Height);
            spawnCoords = spawnCoords.Offset(_random.NextVector2(distancePadding + group.MinimumDistance, distancePadding + group.MaximumDistance));
        }

        _mapSystem.CreateMap(out var mapId);

        var spawnedGrid = _mapManager.CreateGridEntity(mapId);

        _transform.SetMapCoordinates(spawnedGrid, new MapCoordinates(Vector2.Zero, mapId));
        _dungeon.GenerateDungeon(dungeonProto, spawnedGrid.Owner, spawnedGrid.Comp, Vector2i.Zero, _random.Next(), spawnCoords);

        spawned = spawnedGrid.Owner;
        return true;
    }

    private bool TryGridSpawn(EntityUid targetGrid, EntityUid stationUid, MapId mapId, GridSpawnGroup group, out EntityUid spawned)
    {
        spawned = EntityUid.Invalid;

        if (group.Paths.Count == 0)
        {
            Log.Error($"Found no paths for GridSpawn");
            return false;
        }

        var paths = new ValueList<ResPath>();

        // Round-robin so we try to avoid dupes where possible.
        if (paths.Count == 0)
        {
            paths.AddRange(group.Paths);
            _random.Shuffle(paths);
        }

        var path = paths[^1];
        paths.RemoveAt(paths.Count - 1);

        if (_loader.TryLoadGrid(mapId, path, out var grid))
        {
            if (HasComp<ShuttleComponent>(grid))
                TryFTLProximity(grid.Value, targetGrid);

            if (group.NameGrid)
            {
                var name = path.FilenameWithoutExtension;
                _metadata.SetEntityName(grid.Value, name);
            }

            spawned = grid.Value;
            return true;
        }

        Log.Error($"Error loading gridspawn for {ToPrettyString(stationUid)} / {path}");
        return false;
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
        _mapSystem.CreateMap(out var mapId);

        foreach (var group in component.Groups.Values)
        {
            var count = _random.Next(group.MinCount, group.MaxCount + 1);

            for (var i = 0; i < count; i++)
            {
                EntityUid spawned;

                switch (group)
                {
                    case DungeonSpawnGroup dungeon:
                        if (!TryDungeonSpawn(targetGrid.Value, dungeon, out spawned))
                            continue;

                        break;
                    case GridSpawnGroup grid:
                        if (!TryGridSpawn(targetGrid.Value, uid, mapId, grid, out spawned))
                            continue;

                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (_protoManager.TryIndex(group.NameDataset, out var dataset))
                {
                    _metadata.SetEntityName(spawned, _salvage.GetFTLName(dataset, _random.Next()));
                }

                if (group.Hide)
                {
                    var iffComp = EnsureComp<IFFComponent>(spawned);
                    iffComp.Flags |= IFFFlags.HideLabel;
                    Dirty(spawned, iffComp);
                }

                if (group.StationGrid)
                {
                    _station.AddGridToStation(uid, spawned);
                }

                EntityManager.AddComponents(spawned, group.AddComponents);
            }
        }

        _mapSystem.DeleteMap(mapId);
    }

    private void OnGridFillMapInit(EntityUid uid, GridFillComponent component, MapInitEvent args)
    {
        if (!_cfg.GetCVar(CCVars.GridFill))
            return;

        if (!TryComp<DockingComponent>(uid, out var dock) ||
            !TryComp(uid, out TransformComponent? xform) ||
            xform.GridUid == null)
        {
            return;
        }

        // Spawn on a dummy map and try to dock if possible, otherwise dump it.
        _mapSystem.CreateMap(out var mapId);
        var valid = false;

        if (_loader.TryLoadGrid(mapId, component.Path, out var grid))
        {
            var escape = GetSingleDock(grid.Value);

            if (escape != null)
            {
                var config = _dockSystem.GetDockingConfig(grid.Value, xform.GridUid.Value, escape.Value.Entity, escape.Value.Component, uid, dock);

                if (config != null)
                {
                    var shuttleXform = Transform(grid.Value);
                    FTLDock((grid.Value, shuttleXform), config);

                    if (TryComp<StationMemberComponent>(xform.GridUid, out var stationMember))
                    {
                        _station.AddGridToStation(stationMember.Station, grid.Value);
                    }

                    valid = true;
                }
            }

            foreach (var compReg in component.AddComponents.Values)
            {
                var compType = compReg.Component.GetType();

                if (HasComp(grid.Value, compType))
                    continue;

                var comp = _factory.GetComponent(compType);
                AddComp(grid.Value, comp, true);
            }
        }

        if (!valid)
        {
            Log.Error($"Error loading gridfill dock for {ToPrettyString(uid)} / {component.Path}");
        }

        _mapSystem.DeleteMap(mapId);
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
