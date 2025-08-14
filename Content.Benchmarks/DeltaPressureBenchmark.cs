using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.Components;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Benchmarks;

/// <summary>
/// Spawns N number of entities with a <see cref="DeltaPressureComponent"/> and
/// simulates them for a number of ticks M.
/// </summary>
[Virtual]
public class DeltaPressureBenchmark
{
    private TestPair _pair = default!;

    /// <summary>
    /// Number of entities (windows, really) to spawn with a <see cref="DeltaPressureComponent"/>.
    /// </summary>
    [Params(1, 10, 100, 1000, 5000, 10000, 50000)]
    public int EntityCount;

    /// <summary>
    /// Number of ticks to simulate the entities for.
    /// </summary>
    [Params(30)]
    public int Ticks;

    private readonly EntProtoId _windowProtoId = "Window";
    private readonly EntProtoId _wallProtoId = "WallReinforced";

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        var mapdata = await _pair.CreateTestMap();

        /*
         Basically, we want to have a 5-wide grid of tiles.
         Edges are walled, and the length of the grid is determined by N + 2.
         Windows should only touch the top and bottom walls, and each other.
         */

        var entMan = server.EntMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var tileDefMan = server.ResolveDependency<ITileDefinitionManager>();
        var plating = tileDefMan["Plating"].TileId;

        var length = EntityCount + 2; // ensures we can spawn exactly N windows between side walls
        const int height = 5;

        await server.WaitPost(() =>
        {
            // Fill required tiles (extend grid) with plating
            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    mapSys.SetTile(mapdata.Grid, mapdata.Grid, new Vector2i(x, y), new Tile(plating));
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
                        entMan.SpawnEntity(_wallProtoId, coords);
                        continue;
                    }

                    // Spawn windows only on the middle row, spanning interior (excluding side walls)
                    if (y == midY)
                    {
                        entMan.SpawnEntity(_windowProtoId, coords);
                    }
                }
            }
        });

        // Next we run the fixgridatmos command to ensure that we have some air on our grid.
        // Wait a little bit as well.
        // TODO: Unhardcode command magic string when fixgridatmos is an actual command we can ref and not just
        // a stamp-on in AtmosphereSystem.
        await _pair.WaitCommand("fixgridatmos " + mapdata.Grid.Owner, 15);
    }

    [Benchmark]
    public async Task PerformAtmosSimulation()
    {
        var server = _pair.Server;
        await server.WaitRunTicks(Ticks);
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
