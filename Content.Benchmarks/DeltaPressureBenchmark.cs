using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
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
/// Spawns N number of entities with a <see cref="DeltaPressureComponent"/> and
/// simulates them for a number of ticks M.
/// </summary>
[Virtual]
[GcServer(true)]
//[MemoryDiagnoser]
//[ThreadingDiagnoser]
public class DeltaPressureBenchmark
{
    /// <summary>
    /// Number of entities (windows, really) to spawn with a <see cref="DeltaPressureComponent"/>.
    /// </summary>
    [Params(1, 10, 100, 1000, 5000, 10000, 50000, 100000)]
    public int EntityCount;

    /// <summary>
    /// Number of entities that each parallel processing job will handle.
    /// </summary>
    // [Params(1, 10, 100, 1000, 5000, 10000)] For testing how multithreading parameters affect performance (THESE TESTS TAKE 16+ HOURS TO RUN)
    [Params(10)]
    public int BatchSize;

    /// <summary>
    /// Number of entities to process per iteration in the DeltaPressure
    /// processing loop.
    /// </summary>
    // [Params(100, 1000, 5000, 10000, 50000)]
    [Params(1000)]
    public int EntitiesPerIteration;

    private readonly EntProtoId _windowProtoId = "Window";
    private readonly EntProtoId _wallProtoId = "WallPlastitaniumIndestructible";

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

        _cvar.SetCVar(CCVars.DeltaPressureParallelToProcessPerIteration, EntitiesPerIteration);
        _cvar.SetCVar(CCVars.DeltaPressureParallelBatchSize, BatchSize);

        var plating = _tileDefMan["Plating"].TileId;

        /*
         Basically, we want to have a 5-wide grid of tiles.
         Edges are walled, and the length of the grid is determined by N + 2.
         Windows should only touch the top and bottom walls, and each other.
         */

        var length = EntityCount + 2; // ensures we can spawn exactly N windows between side walls
        const int height = 5;

        await server.WaitPost(() =>
        {
            // Fill required tiles (extend grid) with plating
            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    _map.SetTile(mapdata.Grid, mapdata.Grid, new Vector2i(x, y), new Tile(plating));
                }
            }

            // Spawn perimeter walls and windows row in the middle (y = 2)
            const int midY = height / 2;
            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var coords = new EntityCoordinates(mapdata.Grid, x + 0.5f, y + 0.5f);

                    var isPerimeter = x == 0 || x == length - 1 || y == 0 || y == height - 1;
                    if (isPerimeter)
                    {
                        _entMan.SpawnEntity(_wallProtoId, coords);
                        continue;
                    }

                    // Spawn windows only on the middle row, spanning interior (excluding side walls)
                    if (y == midY)
                    {
                        _entMan.SpawnEntity(_windowProtoId, coords);
                    }
                }
            }
        });

        // Next we run the fixgridatmos command to ensure that we have some air on our grid.
        // Wait a little bit as well.
        // TODO: Unhardcode command magic string when fixgridatmos is an actual command we can ref and not just
        // a stamp-on in AtmosphereSystem.
        await _pair.WaitCommand("fixgridatmos " + mapdata.Grid.Owner, 1);

        var uid = mapdata.Grid.Owner;
        _testEnt = new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(
            uid,
            _entMan.GetComponent<GridAtmosphereComponent>(uid),
            _entMan.GetComponent<GasTileOverlayComponent>(uid),
            _entMan.GetComponent<MapGridComponent>(uid),
            _entMan.GetComponent<TransformComponent>(uid));
    }

    [Benchmark]
    public async Task PerformFullProcess()
    {
        await _pair.Server.WaitPost(() =>
        {
            while (!_atmospereSystem.RunProcessingStage(_testEnt, AtmosphereProcessingState.DeltaPressure)) { }
        });
    }

    [Benchmark]
    public async Task PerformSingleRunProcess()
    {
        await _pair.Server.WaitPost(() =>
        {
            _atmospereSystem.RunProcessingStage(_testEnt, AtmosphereProcessingState.DeltaPressure);
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
