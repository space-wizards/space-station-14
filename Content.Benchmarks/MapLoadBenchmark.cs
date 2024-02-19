using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Maps;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Benchmarks;

[Virtual]
public class MapLoadBenchmark
{
    private TestPair _pair = default!;
    private MapLoaderSystem _mapLoader = default!;
    private IMapManager _mapManager = default!;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup(null);

        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        var server = _pair.Server;

        Paths = server.ResolveDependency<IPrototypeManager>()
            .EnumeratePrototypes<GameMapPrototype>()
            .ToDictionary(x => x.ID, x => x.MapPath.ToString());

        _mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
        _mapManager = server.ResolveDependency<IMapManager>();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    public static readonly string[] MapsSource = { "Empty", "Box", "Aspid", "Bagel", "Dev", "CentComm", "Atlas", "Core", "TestTeg", "Saltern", "Packed", "Omega", "Cluster", "Gemini", "Reach", "Origin", "Meta", "Marathon", "Europa", "MeteorArena", "Fland", "Barratry" };

    [ParamsSource(nameof(MapsSource))]
    public string Map;

    public Dictionary<string, string> Paths;

    [Benchmark]
    public async Task LoadMap()
    {
        var mapPath = Paths[Map];
        var server = _pair.Server;
        await server.WaitPost(() =>
        {
            var success = _mapLoader.TryLoad(new MapId(10), mapPath, out _);
            if (!success)
                throw new Exception("Map load failed");
        });
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var server = _pair.Server;
        server.WaitPost(() =>
        {
            _mapManager.DeleteMap(new MapId(10));
        }).Wait();
    }
}
