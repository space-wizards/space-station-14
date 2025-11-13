using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Benchmarks;

/// <summary>
/// Spawns a number of variable-size grids and processes update gas overlay data on them.
/// Does NOT capture updating player sessions with new data.
/// </summary>
[Virtual]
public class GasOverlayBenchmark
{
    [Params(1, 32, 64, 128)]
    public int GridCount;

    /// <summary>
    /// Number of tiles on each grid.
    /// </summary>
    [Params(1, 30, 65, 100, 220)]
    public int GridLength;

    private TestPair _pair = default!;
    private EntityManager _entMan = default!; // We need the actual non-interface entman as it exposes transform query.
    private ITileDefinitionManager _tileDefMan = default!;
    private IMapManager _mapManager = default!;

    private TransformSystem _transformSystem = default!;
    private SharedMapSystem _mapSystem = default!;
    private GasTileOverlaySystem _gasTileOverlaySystem = default!;

    /// <summary>
    /// Returns a length of tiles as a list of tiles; tiles going in a straight line. Starts from the far left.
    /// </summary>
    private static List<(Vector2i, Tile)> ConstructTileLength(int length, in Tile tile)
    {
        var matrix = new List<(Vector2i, Tile)>();
        for (var x = 0; x < length; x++)
        {
            matrix.Add((new Vector2i(x, 0), tile));
        }

        return matrix;
    }

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
            _mapSystem.CreateMap(out var mapId);

            for (; generated <= GridCount; generated++)
            {
                var grid = _mapManager.CreateGridEntity(mapId);

                // This will actually be off-center (iirc) considering bottom-left tile origin. But that doesn't matter here.
                _transformSystem.SetWorldPosition(
                    (grid, _entMan.TransformQuery.GetComponent(grid)),
                    new Vector2(generated * (GridLength + 1), 0)); // Space them apart by 1 tile or something it doesnt really matter how much

                _mapSystem.SetTiles(grid, ConstructTileLength(GridLength, new Tile(plating)));
            }

            InvalidateAllTilesAllGrids();
        });
    }

    // microbenchmark obliterator 9000
    [IterationSetup]
    public async Task IterationSetupAsync()
    {
        await _pair.Server.WaitPost(InvalidateAllTilesAllGrids);
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

    /// <summary>
    /// Invalidates all tiles on all grids.
    /// </summary>
    private void InvalidateAllTilesAllGrids()
    {
        var gridQuery = _entMan.EntityQueryEnumerator<GridAtmosphereComponent, GasTileOverlayComponent>();
        while (gridQuery.MoveNext(out var gridAtmosphereComponent, out var overlayComp))
        {
            foreach (var tile in gridAtmosphereComponent.Tiles)
            {
                overlayComp.InvalidTiles.Add(tile.Key);
            }
        }
    }
}
