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

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    /*
     * Handles PostGen code for marker layers + biomes.
     */

    private async Task PostGen(
        BiomePostGen postGen,
        Dungeon dungeon,
        Entity<MapGridComponent> grid,
        Random random)
    {
        if (_entManager.TryGetComponent(grid, out BiomeComponent? biomeComp))
            return;

        biomeComp = _entManager.AddComponent<BiomeComponent>(grid);
        var biomeSystem = _entManager.System<BiomeSystem>();
        biomeSystem.SetTemplate(grid, biomeComp, _prototype.Index(postGen.BiomeTemplate));
        var seed = random.Next();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var node in dungeon.RoomTiles)
        {
            // Need to set per-tile to override data.
            if (biomeSystem.TryGetTile(node, biomeComp.Layers, seed, grid, out var tile))
            {
                _maps.SetTile(grid, grid, node, tile.Value);
            }

            if (biomeSystem.TryGetDecals(node, biomeComp.Layers, seed, grid, out var decals))
            {
                foreach (var decal in decals)
                {
                    _decals.TryAddDecal(decal.ID, new EntityCoordinates(grid, decal.Position), out _);
                }
            }

            if (biomeSystem.TryGetEntity(node, biomeComp, grid, out var entityProto))
            {
                var ent = _entManager.SpawnEntity(
                    entityProto,
                    new EntityCoordinates(grid, node + grid.Comp.TileSizeHalfVector));
                var xform = xformQuery.Get(ent);

                if (!xform.Comp.Anchored)
                {
                    _transform.AnchorEntity(ent, xform);
                }

                // TODO: Engine bug with SpawnAtPosition
                DebugTools.Assert(xform.Comp.Anchored);
            }

            await SuspendIfOutOfTime();
            ValidateResume();
        }

        biomeComp.Enabled = false;
    }

    private async Task PostGen(
        BiomeMarkerLayerPostGen postGen,
        Dungeon dungeon,
        Entity<MapGridComponent> grid,
        Random random)
    {
        if (!_entManager.TryGetComponent(grid, out BiomeComponent? biomeComp))
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

            await SuspendIfOutOfTime();
            ValidateResume();

            biomeSystem.GetMarkerNodes(grid, biomeComp, grid, markerTemplate, true, bounds, count,
                random, out var spawnSet, out var existing, false);

            await SuspendIfOutOfTime();
            ValidateResume();

            foreach (var ent in existing)
            {
                _entManager.DeleteEntity(ent);
            }

            await SuspendIfOutOfTime();
            ValidateResume();

            foreach (var (node, mask) in spawnSet)
            {
                string? proto;

                if (mask != null && markerTemplate.EntityMask.TryGetValue(mask, out var maskedProto))
                {
                    proto = maskedProto;
                }
                else
                {
                    proto = markerTemplate.Prototype;
                }

                var ent = _entManager.SpawnAtPosition(proto, new EntityCoordinates(grid, node + grid.Comp.TileSizeHalfVector));
                var xform = xformQuery.Get(ent);

                if (!xform.Comp.Anchored)
                    _transform.AnchorEntity(ent, xform);

                await SuspendIfOutOfTime();
                ValidateResume();
            }
        }
    }
}
