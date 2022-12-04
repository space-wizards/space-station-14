using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CPUJob.JobQueues;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage;

public sealed class SalvageCaveJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _protoManager;
    private readonly ITileDefinitionManager _tileDefManager;

    private readonly SalvageExpeditionPrototype _prototype;
    private readonly EntityUid _uid;
    private readonly MapGridComponent _grid;
    private readonly SalvageExpeditionComponent _expedition;
    private readonly SalvageCaveGen _cave;
    private readonly Random _random;

    public SalvageCaveJob(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        ITileDefinitionManager tileDefManager,
        EntityUid uid,
        MapGridComponent grid,
        SalvageExpeditionComponent expedition,
        SalvageExpeditionPrototype prototype,
        SalvageCaveGen cave,
        double maxTime,
        Random random,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _protoManager = protoManager;
        _tileDefManager = tileDefManager;
        _uid = uid;
        _grid = grid;
        _expedition = expedition;
        _prototype = prototype;
        _cave = cave;
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

        var factionId = _prototype.Factions[_random.Next(_prototype.Factions.Count)];
        var faction = _protoManager.Index<SalvageFactionPrototype>(factionId);

        if (!faction.Configs.TryGetValue(_prototype.ID, out var baseFactionConfig) ||
            baseFactionConfig is not SalvageStructureFaction factionConfig)
        {
            return false;
        }

        // We'll do the CA generation up front as we can do that pretty quickly
        // All of the spawning and other setup we'll run over multiple ticks as it will likely go over.

        // Salvage stuff
        var width = _cave.Width;
        var height = _cave.Height;
        var length = width * height;

        // CA stuff
        var solidChance = _cave.SolidChance;

        // Need to double buffer
        var cellDimensions = width * height;

        var cells1 = ArrayPool<bool>.Shared.Rent(cellDimensions);
        var cells2 = ArrayPool<bool>.Shared.Rent(cellDimensions);

        for (var i = 0; i < length; i++)
        {
            if (_random.NextDouble() <= solidChance)
            {
                cells1[i] = true;
            }
            else
            {
                cells1[i] = false;
            }
        }

        var simSteps = _cave.Steps;
        var useCells = cells1;

        for (var i = 0; i < simSteps; i++)
        {
            // Swap the double buffer
            if (i % 2 != 0)
            {
                (cells1, cells2) = (cells2, cells1);
                useCells = cells2;
            }

            Step(cells1, cells2, width, height);
        }

        var tiles = new List<(Vector2i Indices, Tile Tile)>(cells1.Length);
        var tileId = new Tile(_tileDefManager["FloorAsteroidIronsand1"].TileId);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var cell = useCells[y * height + x];

                if (!cell)
                    continue;

                tiles.Add(new ValueTuple<Vector2i, Tile>(new Vector2i(x, y), tileId));
            }
        }

        _grid.SetTiles(tiles);
        ArrayPool<bool>.Shared.Return(cells1);
        ArrayPool<bool>.Shared.Return(cells2);

        // Don't pause pathfinding etc. so we can amortise the costs over several ticks rather than a lump sum
        // at the end.

        for (var i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];

            _entManager.SpawnEntity("AsteroidRock", new EntityCoordinates(_uid, (tile.Indices + new Vector2(0.5f, 0.5f)) * _grid.TileSize));

            if (i % 20 == 0)
            {
                await SuspendIfOutOfTime();

                if (!ValidateResume())
                    return false;
            }
        }

        // Place mob spawn markers
        // TODO: HELPERS
        var frontier = new Queue<Vector2i>(8);
        var checkedTiles = new HashSet<Vector2i>(8);

        for (var i = 0; i < 20; i++)
        {
            frontier.Enqueue(new Vector2i(_random.Next(_cave.Width), _random.Next(_cave.Height)));
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
            // TODO: Spawn marker
            _expedition.SpawnMarkers.Add(_entManager.SpawnEntity("SalvageSpawnMarker", position));

            frontier.Clear();
            checkedTiles.Clear();

            await SuspendIfOutOfTime();
            if (!ValidateResume())
                return false;
        }

        await SuspendIfOutOfTime();
        if (!ValidateResume())
            return false;

        // Spawn mission objectives.
        if (_prototype.Expedition is SalvageStructure structure)
        {
            var count = _random.Next(structure.MinStructures, structure.MaxStructures);

            for (var i = 0; i < count; i++)
            {
                frontier.Enqueue(new Vector2i(_random.Next(_cave.Width), _random.Next(_cave.Height)));
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
                if (!ValidateResume())
                    return false;
            }
        }

        _expedition.Phase = SalvagePhase.Initializing;
        return true;
    }

    private bool ValidateResume()
    {
        return !_entManager.Deleted(_uid) && !_expedition.Deleted;
    }

    private void Step(bool[] cells1, bool[] cells2, int width, int height)
    {
        // TODO: Config
        var aliveLimit = 4;
        var deadLimit = 3;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                // Count the cell neighbours
                var count = 0;

                for (var i = -1; i < 2; i++)
                {
                    for (var j = -1; j < 2; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        var neighborX = x + i;
                        var neighborY = y + j;

                        // Out of bounds
                        if (neighborX < 0 || neighborX >= width ||
                            neighborY < 0 || neighborY >= height)
                        {
                            continue;
                        }

                        if (cells1[neighborY * height + neighborX])
                        {
                            count++;
                        }
                    }
                }

                // Alright now check the thresholds to see what to do with the cell
                var index = y * height + x;
                var alive = cells1[index];

                if (alive)
                {
                    if (count < deadLimit)
                    {
                        cells2[index] = false;
                    }
                    else
                    {
                        cells2[index] = true;
                    }
                }
                else
                {
                    if (count > aliveLimit)
                    {
                        cells2[index] = true;
                    }
                    else
                    {
                        cells2[index] = false;
                    }
                }
            }
        }
    }
}
