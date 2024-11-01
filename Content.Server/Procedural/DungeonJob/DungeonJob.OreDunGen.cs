using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Components;
using Content.Shared.Procedural.DungeonLayers;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="OreDunGen"/>
    /// </summary>
    private async Task PostGen(
        OreDunGen gen,
        Dungeon dungeon,
        Random random)
    {
        // Doesn't use dungeon data because layers and we don't need top-down support at the moment.

        var replaceEntities = new Dictionary<Vector2i, EntityUid>();
        var availableTiles = new List<Vector2i>();
        var tiles = _maps.GetAllTilesEnumerator(_gridUid, _grid);

        while (tiles.MoveNext(out var tileRef))
        {
            var tile = tileRef.Value.GridIndices;

            //Tile mask filtering
            if (gen.TileMask is not null)
            {
                if (!gen.TileMask.Contains(((ContentTileDefinition) _tileDefManager[tileRef.Value.Tile.TypeId]).ID))
                    continue;

                //If entity mask null - we ignore the tiles that have anything on them.
                if (gen.EntityMask is null && !_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    continue;
            }

            //Entity mask filtering
            if (gen.EntityMask is not null)
            {
                var found = false;
                var enumerator2 = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, tile);
                while (enumerator2.MoveNext(out var uid))
                {
                    var prototype = _entManager.GetComponent<MetaDataComponent>(uid.Value).EntityPrototype;

                    if (prototype?.ID is null)
                        continue;

                    if (!gen.EntityMask.Contains(prototype.ID))
                        continue;

                    replaceEntities[tile] = uid.Value;
                    found = true;
                }

                if (!found)
                    continue;
            }

            // Add it to valid nodes.
            availableTiles.Add(tile);

            await SuspendDungeon();

            if (!ValidateResume())
                return;
        }

        var remapping = new Dictionary<EntProtoId, EntProtoId>();

        // TODO: Move this to engine
        if (_prototype.TryIndex(gen.Entity, out var proto) &&
            proto.Components.TryGetComponent("EntityRemap", out var comps))
        {
            var remappingComp = (EntityRemapComponent) comps;
            remapping = remappingComp.Mask;
        }

        var frontier = new ValueList<Vector2i>(32);

        // Iterate the group counts and pathfind out each group.
        for (var i = 0; i < gen.Count; i++)
        {
            await SuspendDungeon();

            if (!ValidateResume())
                return;

            var groupSize = random.Next(gen.MinGroupSize, gen.MaxGroupSize + 1);

            // While we have remaining tiles keep iterating
            while (groupSize > 0 && availableTiles.Count > 0)
            {
                var startNode = random.PickAndTake(availableTiles);
                frontier.Clear();
                frontier.Add(startNode);

                // This essentially may lead to a vein being split in multiple areas but the count matters more than position.
                while (frontier.Count > 0 && groupSize > 0)
                {
                    // Need to pick a random index so we don't just get straight lines of ores.
                    var frontierIndex = random.Next(frontier.Count);
                    var node = frontier[frontierIndex];
                    frontier.RemoveSwap(frontierIndex);
                    availableTiles.Remove(node);

                    // Add neighbors if they're valid, worst case we add no more and pick another random seed tile.
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            var neighbor = new Vector2i(node.X + x, node.Y + y);

                            if (frontier.Contains(neighbor) || !availableTiles.Contains(neighbor))
                                continue;

                            frontier.Add(neighbor);
                        }
                    }

                    var prototype = gen.Entity;

                    if (replaceEntities.TryGetValue(node, out var existingEnt))
                    {
                        var existingProto = _entManager.GetComponent<MetaDataComponent>(existingEnt).EntityPrototype;
                        _entManager.DeleteEntity(existingEnt);

                        if (existingProto != null && remapping.TryGetValue(existingProto.ID, out var remapped))
                        {
                            prototype = remapped;
                        }
                    }

                    // Tile valid salad so add it.
                    _entManager.SpawnAtPosition(prototype, _maps.GridTileToLocal(_gridUid, _grid, node));

                    groupSize--;
                }
            }

            if (groupSize > 0)
            {
                _sawmill.Warning($"Found remaining group size for ore veins of {gen.Entity.Id ?? "null"}!");
            }
        }
    }
}
