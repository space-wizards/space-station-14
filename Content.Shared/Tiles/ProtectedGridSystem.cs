using System.Linq;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;

namespace Content.Shared.Tiles;

public sealed class ProtectedGridSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProtectedGridComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ProtectedGridComponent, FloorTileAttemptEvent>(OnFloorTileAttempt);
    }

    private void OnMapInit(Entity<ProtectedGridComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MapGridComponent>(ent, out var grid))
            return;

        // Engine default is currently 16x size chunks which means we can't just easily have 64bit flags.
        var chunkEnumerator = new ChunkIndicesEnumerator(grid.LocalAABB, 8);

        while (chunkEnumerator.MoveNext(out var chunk))
        {
            ulong flag = 0;

            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var index = new Vector2i(x + chunk.Value.X * 8, y + chunk.Value.Y * 8);
                    var tile = _map.GetTileRef(ent.Owner, grid, index);

                    if (tile.Tile.IsEmpty)
                        continue;

                    var data = SharedMapSystem.ToBitmask(new Vector2i(x, y));

                    flag |= data;
                }
            }

            if (flag == 0)
                continue;

            ent.Comp.BaseIndices[chunk.Value] = flag;
        }

        Dirty(ent);
    }

    private void OnFloorTileAttempt(Entity<ProtectedGridComponent> ent, ref FloorTileAttemptEvent args)
    {
        var chunkOrigin = SharedMapSystem.GetChunkIndices(args.GridIndices, 8);

        if (!ent.Comp.BaseIndices.TryGetValue(chunkOrigin, out var data))
        {
            args.Cancelled = true;
            return;
        }

        if (SharedMapSystem.FromBitmask(args.GridIndices, data))
        {
            args.Cancelled = true;
        }
    }
}
