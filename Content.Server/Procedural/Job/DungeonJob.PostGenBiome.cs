using System.Threading.Tasks;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.Job;

public sealed partial class DungeonJob
{
    /*
     * Handles PostGen code for marker layers + biomes.
     */

    private async Task PostGen(BiomePostGen postGen, Dungeon dungeon, Random random)
    {
        if (_entManager.TryGetComponent(_gridUid, out BiomeComponent? biomeComp))
            return;

        biomeComp = _entManager.AddComponent<BiomeComponent>(_gridUid);
        var biomeSystem = _entManager.System<BiomeSystem>();
        biomeSystem.SetTemplate(_gridUid, biomeComp, _prototype.Index(postGen.BiomeTemplate));
        var seed = random.Next();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var node in dungeon.RoomTiles)
        {
            // Need to set per-tile to override data.
            if (biomeSystem.TryGetTile(node, biomeComp.Layers, seed, _grid, out var tile))
            {
                _maps.SetTile(_gridUid, _grid, node, tile.Value);
            }

            if (biomeSystem.TryGetDecals(node, biomeComp.Layers, seed, _grid, out var decals))
            {
                foreach (var decal in decals)
                {
                    _decals.TryAddDecal(decal.ID, new EntityCoordinates(_gridUid, decal.Position), out _);
                }
            }

            if (biomeSystem.TryGetEntity(node, biomeComp, _grid, out var entityProto))
            {
                var ent = _entManager.SpawnEntity(entityProto, new EntityCoordinates(_gridUid, node + _grid.TileSizeHalfVector));
                var xform = xformQuery.Get(ent);

                if (!xform.Comp.Anchored)
                {
                    _transform.AnchorEntity(ent, xform);
                }

                // TODO: Engine bug with SpawnAtPosition
                DebugTools.Assert(xform.Comp.Anchored);
            }

            await SuspendDungeon();
            if (!ValidateResume())
                return;
        }

        biomeComp.Enabled = false;
    }

    private async Task PostGen(BiomeMarkerLayerPostGen postGen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!_entManager.TryGetComponent(_gridUid, out BiomeComponent? biomeComp))
            return;

        var biomeSystem = _entManager.System<BiomeSystem>();
        var weightedRandom = _prototype.Index(postGen.MarkerTemplate);
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var templates = new Dictionary<string, int>();

        for (var i = 0; i < postGen.Count; i++)
        {
            var template = weightedRandom.Pick(random);
            var count = templates.GetOrNew(template);
            count++;
            templates[template] = count;
        }

        foreach (var (template, count) in templates)
        {
            var markerTemplate = _prototype.Index<BiomeMarkerLayerPrototype>(template);

            var bounds = new Box2i();

            foreach (var tile in dungeon.RoomTiles)
            {
                bounds = bounds.UnionTile(tile);
            }

            await SuspendDungeon();
            if (!ValidateResume())
                return;

            biomeSystem.GetMarkerNodes(_gridUid, biomeComp, _grid, markerTemplate, true, bounds, count,
                random, out var spawnSet, out var existing, false);

            await SuspendDungeon();
            if (!ValidateResume())
                return;

            var checkTile = reservedTiles.Count > 0;

            foreach (var ent in existing)
            {
                if (checkTile && reservedTiles.Contains(_maps.LocalToTile(_gridUid, _grid, _xformQuery.GetComponent(ent).Coordinates)))
                {
                    continue;
                }

                _entManager.DeleteEntity(ent);

                await SuspendDungeon();
                if (!ValidateResume())
                    return;
            }

            foreach (var (node, mask) in spawnSet)
            {
                if (reservedTiles.Contains(node))
                    continue;

                string? proto;

                if (mask != null && markerTemplate.EntityMask.TryGetValue(mask, out var maskedProto))
                {
                    proto = maskedProto;
                }
                else
                {
                    proto = markerTemplate.Prototype;
                }

                var ent = _entManager.SpawnAtPosition(proto, new EntityCoordinates(_gridUid, node + _grid.TileSizeHalfVector));
                var xform = xformQuery.Get(ent);

                if (!xform.Comp.Anchored)
                    _transform.AnchorEntity(ent, xform);

                await SuspendDungeon();
                if (!ValidateResume())
                    return;
            }
        }
    }
}
