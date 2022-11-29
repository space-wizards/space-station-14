using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Salvage;

public sealed class SalvageJob : Job<bool>
{
    private IEntityManager _entManager;

    private List<(Vector2i Indices, Tile Tile)> _tiles;
    private EntityUid _uid;
    private Random _random;

    public SalvageJob(
        IEntityManager entManager,
        EntityUid uid,
        List<(Vector2i Indices, Tile Tile)> tiles,
        int seed,
        double maxTime,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _uid = uid;
        _tiles = tiles;
        _random = new Random(seed);
    }

    protected override async Task<bool> Process()
    {
        // TODO: Just pause the specific map.
        var pathfinding = _entManager.System<PathfindingSystem>();
        pathfinding.PauseUpdating = true;

        for (var i = 0; i < _tiles.Count; i++)
        {
            var tile = _tiles[i];

            // TODO: Tile size.
            _entManager.SpawnEntity("AsteroidRock", new EntityCoordinates(_uid, tile.Indices + new Vector2(0.5f, 0.5f)));

            if (i % 20 == 0)
                await SuspendIfOutOfTime();
        }

        pathfinding.PauseUpdating = false;
        return true;
    }
}
