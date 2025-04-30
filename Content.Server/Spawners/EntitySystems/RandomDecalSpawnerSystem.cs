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
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDecalSpawnerDistributedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RandomDecalSpawnerScatteredComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(EntityUid uid, RandomDecalSpawnerDistributedComponent component, MapInitEvent args)
    {
        TrySpawn(uid, component);
        if (component.DeleteSpawnerAfterSpawn)
            QueueDel(uid);
    }

    public void OnMapInit(EntityUid uid, RandomDecalSpawnerScatteredComponent component, MapInitEvent args)
    {
        TrySpawn(uid, component);
        if (component.DeleteSpawnerAfterSpawn)
            QueueDel(uid);
    }

    public bool TrySpawn(EntityUid uid, RandomDecalSpawnerDistributedComponent component)
    {
        if (!TryComp<RandomDecalSpawnerDistributedComponent>(uid, out var comp))
            return false;

        if (component == null)
            return false;

        if (component.Decals.Count == 0)
            return false;

        var tileBlacklist = new List<ITileDefinition>();
        if (component.TileBlacklist.Count > 0)
        {
            tileBlacklist = GetTileDefs(component.TileBlacklist);
        }

        // I feel like there's a better way to do this, but I can't find it
        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var localPos = xform.Coordinates.Position;

        var tileRefs = _map.GetLocalTilesIntersecting(
            uid,
            grid,
            new Box2(localPos + new Vector2(-component.Range, -component.Range),
                localPos + new Vector2(component.Range, component.Range))
        );

        foreach (var tileRef in tileRefs)
        {
            var basePosition = new EntityCoordinates(xform.GridUid.Value, tileRef.GridIndices * grid.TileSize);
            if (!basePosition.TryDistance(_entities, new EntityCoordinates(uid, 0f, 0f), out var distance))
                continue;

            for (var i = 0; i < component.MaxDecalsPerTile; i++)
            {
                var position = basePosition;

                if (!component.SnapPosition)
                    // we create this new vector instead of using _random.NextVector because using
                    // _random.NextVector could move the decal out of the tile.
                    position = position.Offset(new Vector2(_random.NextFloat(), _random.NextFloat()));

                SpawnDecal(uid, component, xform.GridUid.Value, grid, position, tileBlacklist);
            }
        }

        return true;
    }

    public bool TrySpawn(EntityUid uid, RandomDecalSpawnerScatteredComponent component)
    {
        if (!TryComp<RandomDecalSpawnerScatteredComponent>(uid, out var comp))
            return false;

        if (component == null)
            return false;

        if (component.Decals.Count == 0)
            return false;

        var tileBlacklist = new List<ITileDefinition>();
        if (component.TileBlacklist.Count > 0)
        {
            tileBlacklist = GetTileDefs(component.TileBlacklist);
        }

        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        for (var i = 0; i < component.MaxDecals; i++)
        {
            // The vector added here is just to center the generated decals to the tile.
            var localPos = xform.Coordinates.Position + _random.NextVector2(component.Range) + new Vector2(-0.5f, -0.5f);
            var basePosition = new EntityCoordinates(xform.GridUid.Value, localPos);
            SpawnDecal(uid, component, xform.GridUid.Value, grid, basePosition, tileBlacklist);
        }

        return true;
    }

    // Gets a list of all the tile definitions in the current map that are also part of the blacklist
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

    private uint SpawnDecal(EntityUid uid, RandomDecalSpawnerComponent component, EntityUid gridUid, MapGridComponent grid, EntityCoordinates position, List<ITileDefinition> tileBlacklist)
    {
        if (component.Prob < 1f && _random.NextFloat() > component.Prob)
            return 0;

        if (tileBlacklist.Count > 0)
        {
            _tileDefs.TryGetDefinition(_map.GetTileRef(gridUid, grid, position).Tile.TypeId, out var currTileDef);
            if (currTileDef is null || tileBlacklist.Contains(currTileDef))
                return 0;
        }

        var rotation = Angle.Zero;
        if (component.RandomRotation)
        {
            if (component.SnapRotation)
                rotation = (MathF.PI / 2f) * _random.Next(3);
            else
                rotation = _random.NextAngle();
        }

        var color = component.Color;
        if (component.RandomColorList.Count != 0)
            color = _random.Pick(component.RandomColorList);

        _decal.TryAddDecal(
            _random.Pick(component.Decals),
            position,
            out var decalId,
            color,
            rotation,
            component.zIndex,
            component.Cleanable
        );

        return decalId;
    }
}
