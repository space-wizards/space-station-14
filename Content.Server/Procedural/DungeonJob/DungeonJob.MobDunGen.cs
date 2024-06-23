using System.Threading.Tasks;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Robust.Shared.Collections;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    private async Task PostGen(
        MobsDunGen gen,
        DungeonData data,
        Dungeon dungeon,
        Random random)
    {
        var availableRooms = new ValueList<DungeonRoom>();
        availableRooms.AddRange(dungeon.Rooms);
        var availableTiles = new ValueList<Vector2i>();

        foreach (var entry in gen.Groups)
        {
            while (availableRooms.Count > 0)
            {
                availableTiles.Clear();
                var roomIndex = random.Next(availableRooms.Count);
                var room = availableRooms.RemoveSwap(roomIndex);
                availableTiles.AddRange(room.Tiles);

                while (availableTiles.Count > 0)
                {
                    var tile = availableTiles.RemoveSwap(random.Next(availableTiles.Count));

                    if (!_anchorable.TileFree(_grid, tile, (int) CollisionGroup.MachineLayer,
                            (int) CollisionGroup.MachineLayer))
                    {
                        continue;
                    }

                    var uid = _entManager.SpawnAtPosition(entry.Proto, _maps.GridTileToLocal(_gridUid, _grid, tile));
                    return;
                }
            }
        }
    }
}
