using System.Threading.Tasks;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="BiomeDunGen"/>
    /// </summary>
    private async Task PostGen(BiomeDunGen dunGen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (_entManager.TryGetComponent(_gridUid, out BiomeComponent? biomeComp))
            return;

        biomeComp = _entManager.AddComponent<BiomeComponent>(_gridUid);
        var biomeSystem = _entManager.System<BiomeSystem>();
        biomeSystem.SetTemplate(_gridUid, biomeComp, _prototype.Index(dunGen.BiomeTemplate));
        var seed = random.Next();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var node in dungeon.RoomTiles)
        {
            if (reservedTiles.Contains(node))
                continue;

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
}
