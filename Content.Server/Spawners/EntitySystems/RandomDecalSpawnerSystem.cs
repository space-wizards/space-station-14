using System.Linq;
using System.Numerics;
using Content.Server.Spawners.Components;
using Content.Server.Decals;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class RandomDecalSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDecalSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(EntityUid uid, RandomDecalSpawnerComponent component, MapInitEvent args)
    {
        TrySpawn(uid, component);
    }

    public bool TrySpawn(EntityUid uid, RandomDecalSpawnerComponent component)
    {
        if (!TryComp<RandomDecalSpawnerComponent>(uid, out var comp))
            return false;

        if (component == null)
            return false;

        var tileBlacklist = new List<ITileDefinition>();
        if (component.TileBlacklist.Count > 0)
        {
            tileBlacklist = GetTileDefs(component.TileBlacklist);
        }

        // there's a better way to do this, but I can't find it and the documentation doesn't have it
        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var localpos = xform.Coordinates.Position;

        var tilerefs = _map.GetLocalTilesIntersecting(
            uid,
            grid,
            new Box2(localpos + new Vector2(-component.Range, -component.Range),
                localpos + new Vector2(component.Range, component.Range))
        );

        foreach (var tileref in tilerefs)
        {
            if (tileBlacklist.Count > 0)
            {
                _tileDefs.TryGetDefinition(tileref.Tile.TypeId, out var currTileDef);
                if (currTileDef is null || tileBlacklist.Contains(currTileDef))
                    continue;
            }

            for (var i = 0; i < component.MaxDecalsPerTile; i++)
            {
                var position = _map.ToCoordinates(tileref, grid);
                if (component.SnapPosition)
                    position = position.Offset(new Vector2(0.5f,0.5f));
                else
                    position = position.Offset(new Vector2(_random.NextFloat(), _random.NextFloat()));

                var rotation = Angle.Zero;
                if (component.RandomRotation)
                {
                    if (component.SnapPosition)
                        rotation = MathF.PI / 2f * _random.Next(3);
                    else
                        rotation = _random.NextAngle();
                }

                if (!position.TryDistance(_entities, new EntityCoordinates(uid, 0f ,0f), out var distance))
                    continue;

                if (component.Falloff == 0 || _random.NextFloat() > (distance/component.Falloff))
                {
                    _decal.TryAddDecal(
                        _random.Pick(component.Decals),
                        position,
                        out var decalId,
                        component.Color,
                        rotation,
                        component.zIndex,
                        component.Cleanable
                    );
                }
            }
        }

        return true;
    }

    // Gets a list of all the tile definitions in the current map that are also part of the whitelist
    // This is so we can minimize the list size we have to work with.
    private List<ITileDefinition> GetTileDefs(List<String> tileNames)
    {
        var existingTileDefs = new List<ITileDefinition>();
        foreach (var tileName in tileNames)
        {
            if (_tileDefs.TryGetDefinition(tileName, out var tileDef))
                existingTileDefs.Add(tileDef);
        }
        return existingTileDefs;
    }
}
