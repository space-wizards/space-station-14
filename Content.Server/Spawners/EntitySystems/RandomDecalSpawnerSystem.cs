using System.Numerics;
using Content.Server.Decals;
using Content.Server.Spawners.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class RandomDecalSpawnerSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDecalSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomDecalSpawnerComponent component, MapInitEvent args)
    {
        TrySpawn(uid);
        if (component.DeleteSpawnerAfterSpawn)
            QueueDel(uid);
    }

    public bool TrySpawn(Entity<RandomDecalSpawnerComponent?> ent)
    {
        if (!TryComp<RandomDecalSpawnerComponent>(ent, out var comp))
            return false;

        if (comp.Decals.Count == 0)
            return false;

        var tileWhitelist = new List<ITileDefinition>();
        if (comp.TileWhitelist.Count > 0)
        {
            foreach (var tileProto in comp.TileWhitelist)
            {
                if (_tileDefs.TryGetDefinition(tileProto, out var tileDef))
                    tileWhitelist.Add(tileDef);
            }
        }
        else if (comp.TileBlacklist.Count > 0)
        {
            foreach (var tileDef in _tileDefs)
            {
                if (!comp.TileBlacklist.Contains(tileDef.ID))
                    tileWhitelist.Add(tileDef);
            }
        }

        var xform = Transform(ent);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var addedDecals = new Dictionary<string, int>();

        for (var i = 0; i < comp.MaxDecals; i++)
        {
            if (comp.Prob < 1f && _random.NextFloat() > comp.Prob)
                continue;

            // The vector added here is just to center the generated decals to the tile the spawner is on.
            var localPos = xform.Coordinates.Position + _random.NextVector2(comp.Radius) + new Vector2(-0.5f, -0.5f);
            var position = new EntityCoordinates(xform.GridUid.Value, localPos);

            var tileRef = _map.GetTileRef(xform.GridUid.Value, grid, position);

            if (tileWhitelist.Count > 0)
            {
                _tileDefs.TryGetDefinition(tileRef.Tile.TypeId, out var currTileDef);
                if (currTileDef is null || !tileWhitelist.Contains(currTileDef))
                    continue;
            }

            var tileRefStr = tileRef.ToString();
            if (comp.MaxDecalsPerTile is > 0)
            {
                addedDecals.TryAdd(tileRefStr, 0);
                if (addedDecals[tileRefStr] >= comp.MaxDecalsPerTile)
                    continue;
            }

            var decalProtoId = _random.Pick(comp.Decals);
            var decalProto = _prototypes.Index(decalProtoId);
            var snapPosition = comp.SnapPosition ?? decalProto.DefaultSnap;
            if (snapPosition)
            {
                position = position.WithPosition(tileRef.GridIndices * grid.TileSize);
            }

            var cleanable = comp.Cleanable ?? decalProto.DefaultCleanable;

            var rotation = Angle.Zero;
            if (comp.RandomRotation)
            {
                if (comp.SnapRotation)
                    rotation = new Angle((MathF.PI / 2f) * _random.Next(3));
                else
                    rotation = _random.NextAngle();
            }

            var color = comp.Color;
            if (comp.RandomColorList != null && comp.RandomColorList.Count != 0)
                color = _random.Pick(comp.RandomColorList);

            _decal.TryAddDecal(
                decalProtoId,
                position,
                out _,
                color,
                rotation,
                comp.ZIndex,
                cleanable
            );

            if (comp.MaxDecalsPerTile is > 0)
                addedDecals[tileRefStr]++;
        }

        return true;
    }
}
