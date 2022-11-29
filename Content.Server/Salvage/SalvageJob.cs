using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Content.Server.Salvage.Expeditions;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed class SalvageJob : Job<bool>
{
    private IEntityManager _entManager;

    private SalvageExpeditionPrototype _prototype;
    private SalvageFactionPrototype _faction;
    private List<(Vector2i Indices, Tile Tile)> _tiles;
    private EntityUid _uid;
    private MapGridComponent _grid;
    private Random _random;
    private Vector2i _dimensions;

    public SalvageJob(
        IEntityManager entManager,
        EntityUid uid,
        MapGridComponent grid,
        List<(Vector2i Indices, Tile Tile)> tiles,
        Vector2i dimensions,
        SalvageExpeditionPrototype prototype,
        SalvageFactionPrototype faction,
        Random random,
        double maxTime,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _uid = uid;
        _grid = grid;
        _tiles = tiles;
        _dimensions = dimensions;
        _prototype = prototype;
        _faction = faction;
        _random = random;
    }

    protected override async Task<bool> Process()
    {
        switch (_prototype.Expedition)
        {
            case SalvageStructure:
                break;
            default:
                return false;
        }

        if (!_faction.Configs.TryGetValue(_prototype.ID, out var baseFactionConfig) ||
            baseFactionConfig is not SalvageStructureFaction factionConfig)
        {
            return false;
        }

        // Don't pause pathfinding etc. so we can amortise the costs over several ticks rather than a lump sum
        // at the end.

        for (var i = 0; i < _tiles.Count; i++)
        {
            var tile = _tiles[i];

            _entManager.SpawnEntity("AsteroidRock", new EntityCoordinates(_uid, (tile.Indices + new Vector2(0.5f, 0.5f)) * _grid.TileSize));

            if (i % 20 == 0)
                await SuspendIfOutOfTime();
        }

        // TODO: Pick random tiles for mob spawns.

        if (_prototype.Expedition is SalvageStructure structure)
        {
            var count = _random.Next(structure.MinStructures, structure.MaxStructures);
            var checkedTiles = new HashSet<Vector2i>(8);
            var frontier = new Queue<Vector2i>(8);

            for (var i = 0; i < count; i++)
            {
                // TODO: Pathfind from spawn spot out to all structures to validate it.
                frontier.Enqueue(new Vector2i(_random.Next(_dimensions.X), _random.Next(_dimensions.Y)));
                Vector2i pos;

                while (frontier.TryDequeue(out pos))
                {
                    if (!checkedTiles.Add(pos))
                        continue;

                    var enumerator = _grid.GetAnchoredEntitiesEnumerator(pos);

                    if (!enumerator.MoveNext(out _))
                    {
                        break;
                    }

                    // Tile not clear, get neighbours.
                    checkedTiles.Add(pos);

                    // TODO: Don't add out of bounds neighbours.

                    frontier.Enqueue(pos + new Vector2i(-1, 0));
                    frontier.Enqueue(pos + new Vector2i(0, -1));
                    frontier.Enqueue(pos + new Vector2i(1, 0));
                    frontier.Enqueue(pos + new Vector2i(0, 1));
                }

                var position = new EntityCoordinates(_uid, (pos + new Vector2(0.5f, 0.5f)) * _grid.TileSize);
                _entManager.SpawnEntity(factionConfig.Spawn, position);
                frontier.Clear();
                checkedTiles.Clear();

                await SuspendIfOutOfTime();
            }
        }

        return true;
    }
}
