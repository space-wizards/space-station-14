using System.Threading.Tasks;
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
        List<Dungeon> dungeons,
        HashSet<Vector2i> reservedTiles,
        Random random)
    {
        var emptyTiles = false;
        var replaceEntities = new Dictionary<Vector2i, EntityUid>();
        var availableTiles = new List<Vector2i>();
        var remapName = _entManager.ComponentFactory.GetComponentName<EntityRemapComponent>();
        var replacementRemapping = new Dictionary<EntProtoId, EntProtoId>();

        if (_prototype.TryIndex(gen.Replacement, out var replacementProto) &&
            replacementProto.Components.TryGetComponent(remapName, out var replacementComps))
        {
            var remappingComp = (EntityRemapComponent) replacementComps;
            replacementRemapping = remappingComp.Mask;
        }

        if (gen.Replacement != null)
        {
            replacementRemapping[gen.Replacement.Value] = gen.Entity;
        }

        foreach (var dungeon in dungeons)
        {
            foreach (var node in dungeon.AllTiles)
            {
                if (reservedTiles.Contains(node))
                    continue;

                // Empty tile, skip if relevant.
                if (!emptyTiles && (!_maps.TryGetTile(_grid, node, out var tile) || tile.IsEmpty))
                    continue;

                // Check if it's a valid spawn, if so then use it.
                var enumerator = _maps.GetAnchoredEntitiesEnumerator(_gridUid, _grid, node);
                var found = false;

                // We use existing entities as a mark to spawn in place
                // OR
                // We check for any existing entities to see if we can spawn there.
                // We can't replace so just stop here.
                if (gen.Replacement != null)
                {
                    while (enumerator.MoveNext(out var uid))
                    {
                        var prototype = _entManager.GetComponent<MetaDataComponent>(uid.Value).EntityPrototype;

                        if (string.IsNullOrEmpty(prototype?.ID))
                            continue;

                        // It has a valid remapping so take it over.
                        if (replacementRemapping.ContainsKey(prototype.ID))
                        {
                            replaceEntities[node] = uid.Value;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    continue;

                // Add it to valid nodes.
                availableTiles.Add(node);

                await SuspendDungeon();

                if (!ValidateResume())
                    return;
            }
        }

        var remapping = new Dictionary<EntProtoId, EntProtoId>();

        // TODO: Move this to engine
        if (_prototype.TryIndex(gen.Entity, out var proto) &&
            proto.Components.TryGetComponent(remapName, out var comps))
        {
            var remappingComp = (EntityRemapComponent) comps;
            remapping = remappingComp.Mask;
        }

        var frontier = new ValueList<Vector2i>(32);

        // Iterate the group counts and pathfind out each group.
        for (var i = 0; i < gen.Count; i++)
        {
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

                    // May have been deleted while iteration was suspended.
                    if (replaceEntities.TryGetValue(node, out var existingEnt) && _entManager.TryGetComponent(existingEnt, out MetaDataComponent? metadata))
                    {
                        var existingProto = metadata.EntityPrototype;
                        _entManager.DeleteEntity(existingEnt);

                        if (existingProto != null && remapping.TryGetValue(existingProto.ID, out var remapped))
                        {
                            prototype = remapped;
                        }
                    }

                    // Tile valid salad so add it.
                    var uid = _entManager.SpawnAtPosition(prototype, _maps.GridTileToLocal(_gridUid, _grid, node));
                    AddLoadedEntity(node, uid);

                    groupSize--;

                    await SuspendDungeon();

                    if (!ValidateResume())
                        return;
                }
            }

            if (groupSize > 0)
            {
                // Not super worried depending on the gen it's fine.
                _sawmill.Debug($"Found remaining group size for ore veins of {gen.Replacement ?? "null"} / {gen.Entity}!");
            }
        }
    }
}
