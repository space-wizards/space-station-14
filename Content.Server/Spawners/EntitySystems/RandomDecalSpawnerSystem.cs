using System.Numerics;
using System.Collections.Generic;
using Content.Server.Spawners.Components;
using Content.Server.Decals;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

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

        var tileBlacklist = new List<ITileDefinition>();
        if (comp.TileBlacklist.Count > 0)
        {
            tileBlacklist = GetTileDefs(comp.TileBlacklist);
        }

        var xform = Transform(ent);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var addedDecals = new Dictionary<String, int>();

        for (var i = 0; i < comp.MaxDecals; i++)
        {
            if (comp.Prob < 1f && _random.NextFloat() > comp.Prob)
                continue;

            // The vector added here is just to center the generated decals to the tile the spawner is on.
            var localPos = xform.Coordinates.Position + _random.NextVector2(comp.Radius) + new Vector2(-0.5f, -0.5f);
            var position = new EntityCoordinates(xform.GridUid.Value, localPos);

            var tileRef = _map.GetTileRef(xform.GridUid.Value, grid, position);
            var tileRefStr = tileRef.ToString();
            if (comp.MaxDecalsPerTile > 0)
            {
                addedDecals.TryAdd(tileRefStr, 0);
                if (addedDecals[tileRefStr] >= comp.MaxDecalsPerTile)
                    continue;
            }

            if (comp.SnapPosition)
            {
                position = position.WithPosition(tileRef.GridIndices * grid.TileSize);
            }

            if (tileBlacklist.Count > 0)
            {
                _tileDefs.TryGetDefinition(tileRef.Tile.TypeId, out var currTileDef);
                if (currTileDef is null || tileBlacklist.Contains(currTileDef))
                    continue;
            }

            var rotation = Angle.Zero;
            if (comp.RandomRotation)
            {
                if (comp.SnapRotation)
                    rotation = new Angle((MathF.PI / 2f) * _random.Next(3));
                else
                    rotation = _random.NextAngle();
            }

            var color = comp.Color;
            if (comp.RandomColorList.Count != 0)
                color = _random.Pick(comp.RandomColorList);

            _decal.TryAddDecal(
                _random.Pick(comp.Decals),
                position,
                out var decalId,
                color,
                rotation,
                comp.ZIndex,
                comp.Cleanable
            );

            if (comp.MaxDecalsPerTile > 0)
                addedDecals[tileRefStr]++;
        }

        return true;
    }

    // Gets a list of all the tile definitions in the current map that are also part of the blacklist
    // This is so we can minimize the list size we have to work with.
    private List<ITileDefinition> GetTileDefs(List<ProtoId<ContentTileDefinition>> tileProtos)
    {
        var existingTileDefs = new List<ITileDefinition>();
        foreach (var tileProto in tileProtos)
        {
            if (_tileDefs.TryGetDefinition(tileProto, out var tileDef))
                existingTileDefs.Add(tileDef);
        }

        return existingTileDefs;
    }
}
