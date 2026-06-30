using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.NPC.Pathfinding;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Benchmarks;

[Virtual]
[MemoryDiagnoser]
public class PathfindingSystemBenchmark
{
    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private SharedMapSystem _map = default!;
    private PathfindingSystem _pathfinding = default!;

    private Entity<MapGridComponent> _grid;
    private GridPathfindingChunk _chunk = default!;
    private MapId _mapId;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient(testContext: new ExternalTestContext("Benchmark", StreamWriter.Null));

        var server = _pair.Server;
        _entMan = server.ResolveDependency<IEntityManager>();
        _map = _entMan.System<SharedMapSystem>();

        await server.WaitPost(() =>
        {
            _map.CreateMap(out _mapId);
            _grid = _map.CreateGridEntity(_mapId);

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    _map.SetTile(_grid, new Vector2i(x, y), new Tile(1));
                }
            }

            _entMan.SpawnEntity("WallReinforced", _map.GridTileToLocal(_grid, _grid.Comp, new Vector2i(0, 1)));
            _entMan.SpawnEntity("WallReinforced", _map.GridTileToLocal(_grid, _grid.Comp, new Vector2i(1, 0)));
        });

        _pathfinding = _entMan.System<PathfindingSystem>();
        _chunk = new GridPathfindingChunk
        {
            Origin = new Vector2i(0, 0)
        };

    }

    [IterationSetup]
    public void IterSetup()
    {
        _chunk = new GridPathfindingChunk
        {
            Origin = new Vector2i(0, 0)
        };
    }

    [Benchmark(Baseline = true)]
    public void BuildBreadcrumbsOld()
    {
        _pathfinding.BuildBreadcrumbsOld(_chunk, _grid);
    }

    [Benchmark]
    public void BuildBreadcrumbs()
    {
        _pathfinding.BuildBreadcrumbs(_chunk, _grid);
    }
}
