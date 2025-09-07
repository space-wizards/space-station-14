using System.Numerics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Benchmarks;

/// <summary>
/// Spawns a number of 1x1-tile grids and processing update gas overlay data on them.
/// Does NOT capture updating player sessions with new data.
/// </summary>
[Virtual]
//[MemoryDiagnoser]
public class GasOverlayBenchmark
{
    [Params(1, 2, 4, 8, 10, 25, 50, 100)]
    public int GridCount;

    private TestPair _pair = default!;
    private EntityManager _entMan = default!; // We need the actual non-interface entman as it exposes transform query.
    private ITileDefinitionManager _tileDefMan = default!;
    private IMapManager _mapManager = default!;

    private TransformSystem _transformSystem = default!;
    private SharedMapSystem _mapSystem = default!;
    private GasTileOverlaySystem _gasTileOverlaySystem = default!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        _entMan = server.ResolveDependency<EntityManager>();
        _tileDefMan = server.ResolveDependency<ITileDefinitionManager>();
        _mapManager = server.ResolveDependency<IMapManager>();

        _transformSystem = _entMan.System<TransformSystem>();
        _mapSystem = _entMan.System<SharedMapSystem>();
        _gasTileOverlaySystem = _entMan.System<GasTileOverlaySystem>();

        var plating = _tileDefMan["Plating"].TileId;
        var generated = 0; // How many grids we have generated so far.

        await server.WaitPost(() =>
        {
            var mapUid = _mapSystem.CreateMap(out var mapId);

            for (; generated <= GridCount; generated++)
            {
                var grid = _mapManager.CreateGridEntity(mapId);

                // This will actually be off-center (iirc) considering bottom-left tile origin. But that doesn't matter here.
                _transformSystem.SetWorldPosition(
                    (grid, _entMan.TransformQuery.GetComponent(grid)),
                    new Vector2(generated * 2, 0)); // Space them apart a bit just incase.

                // Entire grid is a 1x1 plating.
                _mapSystem.SetTile(grid, new EntityCoordinates(grid, 0, 0), new Tile(plating));
            }
        });
    }

    // No visible gas overlay changes.
    [Benchmark]
    public async Task OverlayDataUpdate()
    {
        await _pair.Server.WaitPost(() =>
        {
            _gasTileOverlaySystem.UpdateOverlayData();
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
