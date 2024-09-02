using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Benchmarks;

/// <summary>
/// This benchmarks spawns several humans, gives them captain equipment and then deletes them.
/// This measures performance for spawning, deletion, containers, and inventory code.
/// </summary>
[Virtual, MemoryDiagnoser]
public class SpawnEquipDeleteBenchmark
{
    private TestPair _pair = default!;
    private StationSpawningSystem _spawnSys = default!;
    private const string Mob = "MobHuman";
    private StartingGearPrototype _gear = default!;
    private EntityUid _entity;
    private EntityCoordinates _coords;

    [Params(1, 4, 16, 64)]
    public int N;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        var mapData = await _pair.CreateTestMap();
        _coords = mapData.GridCoords;
        _spawnSys = server.System<StationSpawningSystem>();
        _gear = server.ProtoMan.Index<StartingGearPrototype>("CaptainGear");
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    [Benchmark]
    public async Task SpawnDeletePlayer()
    {
        await _pair.Server.WaitPost(() =>
        {
            var server = _pair.Server;
            for (var i = 0; i < N; i++)
            {
                _entity = server.EntMan.SpawnAttachedTo(Mob, _coords);
                _spawnSys.EquipStartingGear(_entity, _gear);
                server.EntMan.DeleteEntity(_entity);
            }
        });
    }
}
