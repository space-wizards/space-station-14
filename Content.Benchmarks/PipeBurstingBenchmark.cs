using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Benchmarks;

/// <summary>
/// Spawns N number of pipes on a grid and runs processing on them.
/// </summary>
[Virtual]
[GcServer(true)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
//[MemoryDiagnoser]
//[ThreadingDiagnoser]
public class PipeBurstingBenchmark
{
    /// <summary>
    /// Number of pipes to spawn along a straight line.
    /// </summary>
    [Params(1, 10, 100, 1000, 5000, 10000)]
    public int EntityCount;

    private readonly EntProtoId _gasPipe = "GasPipeStraight";

    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private SharedMapSystem _map = default!;
    private IRobustRandom _random = default!;
    private IConfigurationManager _cvar = default!;
    private ITileDefinitionManager _tileDefMan = default!;
    private AtmosphereSystem _atmospereSystem = default!;

    private Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>
        _testEnt;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        var mapdata = await _pair.CreateTestMap();

        _entMan = server.ResolveDependency<IEntityManager>();
        _map = _entMan.System<SharedMapSystem>();
        _random = server.ResolveDependency<IRobustRandom>();
        _cvar = server.ResolveDependency<IConfigurationManager>();
        _tileDefMan = server.ResolveDependency<ITileDefinitionManager>();
        _atmospereSystem = _entMan.System<AtmosphereSystem>();

        _random.SetSeed(69420); // Randomness needs to be deterministic for benchmarking.

        var plating = _tileDefMan["Plating"].TileId;

        var length = EntityCount + 2;
        const int height = 5;

        await server.WaitPost(delegate
        {
            // Fill required tiles (extend grid) with plating
            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    _map.SetTile(mapdata.Grid, mapdata.Grid, new Vector2i(x, y), new Tile(plating));
                }
            }

            const int midY = height / 2;
            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var coords = new EntityCoordinates(mapdata.Grid, x + 0.5f, y + 0.5f);

                    if (y == midY)
                    {
                        _entMan.SpawnEntity(_gasPipe, coords);
                    }
                }
            }
        });

        var uid = mapdata.Grid.Owner;
        _testEnt = new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(
            uid,
            _entMan.GetComponent<GridAtmosphereComponent>(uid),
            _entMan.GetComponent<GasTileOverlayComponent>(uid),
            _entMan.GetComponent<MapGridComponent>(uid),
            _entMan.GetComponent<TransformComponent>(uid));

        await server.WaitRunTicks(5);
    }

    [Benchmark]
    public async Task PerformFullProcess()
    {
        await _pair.Server.WaitPost(delegate
        {
            while (!_atmospereSystem.RunProcessingStage(_testEnt, AtmosphereProcessingState.PipeNet)) { }
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
