using System.Threading.Tasks;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="BiomeMarkerLayerDunGen"/>
    /// </summary>
    private async Task PostGen(BiomeMarkerLayerDunGen dunGen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        // If we're adding biome then disable it and just use for markers.
        if (_entManager.EnsureComponent(_gridUid, out BiomeComponent biomeComp))
        {
            biomeComp.Enabled = false;
        }

        var biomeSystem = _entManager.System<BiomeSystem>();
        var weightedRandom = _prototype.Index(dunGen.MarkerTemplate);
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var templates = new Dictionary<string, int>();

        for (var i = 0; i < dunGen.Count; i++)
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
