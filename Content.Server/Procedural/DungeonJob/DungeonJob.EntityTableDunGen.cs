using System.Linq;
using System.Threading.Tasks;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Robust.Shared.Collections;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    private async Task PostGen(
        EntityTableDunGen gen,
        List<Dungeon> dungeons,
        HashSet<Vector2i> reservedTiles,
        Random random)
    {
        var count = random.Next(gen.MinCount, gen.MaxCount + 1);
        var npcs = _entManager.System<NPCSystem>();

        foreach (var dungeon in dungeons)
        {
            var availableRooms = new ValueList<DungeonRoom>();
            availableRooms.AddRange(dungeon.Rooms);
            var availableTiles = new ValueList<Vector2i>(dungeon.AllTiles);

            while (availableTiles.Count > 0 && count > 0)
            {
                var tile = availableTiles.RemoveSwap(random.Next(availableTiles.Count));

                await SuspendDungeon();

                if (!ValidateResume())
                    return;

                if (reservedTiles.Contains(tile))
                    continue;

                if (!_anchorable.TileFree(_grid,
                        tile,
                        (int) CollisionGroup.MachineLayer,
                        (int) CollisionGroup.MachineLayer))
                {
                    continue;
                }

                var entities = _entManager.System<EntityTableSystem>().GetSpawns(gen.Table, random).ToList();
                foreach (var ent in entities)
                {
                    var uid = _entManager.SpawnAtPosition(ent, _maps.GridTileToLocal(_gridUid, _grid, tile));
                    _entManager.RemoveComponent<GhostRoleComponent>(uid);
                    _entManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
                    npcs.SleepNPC(uid);
                }

                count--;
            }

            if (gen.PerDungeon)
            {
                count = random.Next(gen.MinCount, gen.MaxCount + 1);
            }
            // Stop if count is 0, otherwise go to next dungeon.
            else if (count == 0)
            {
                return;
            }
        }
    }
}
