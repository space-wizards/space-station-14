using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Shared.Maps;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Benchmarks;

[Virtual]
public class MapLoadBenchmark
{
    private TestPair _pair = default!;
    private MapLoaderSystem _mapLoader = default!;
    private SharedMapSystem _mapSys = default!;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();

        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        var server = _pair.Server;

        Paths = server.ResolveDependency<IPrototypeManager>()
            .EnumeratePrototypes<GameMapPrototype>()
            .ToDictionary(x => x.ID, x => x.MapPath.ToString());

        _mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
        _mapSys = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedMapSystem>();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    public static string[] MapsSource { get; } = { "Empty", "Saltern", "Box", "Bagel", "Dev", "CentComm", "Core", "TestTeg", "Packed", "Omega", "Reach", "Meta", "Marathon", "MeteorArena", "Fland", "Oasis", "Convex"};

    [ParamsSource(nameof(MapsSource))]
    public string Map;

    public Dictionary<string, string> Paths;
    private MapId _mapId;

    [Benchmark]
    public async Task LoadMap()
    {
        var mapPath = new ResPath(Paths[Map]);
        var server = _pair.Server;
        await server.WaitPost(() =>
        {
            var success = _mapLoader.TryLoadMap(mapPath, out var map, out _);
            if (!success)
                throw new Exception("Map load failed");
            _mapId = map.Value.Comp.MapId;
        });
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var server = _pair.Server;
        server.WaitPost(() => _mapSys.DeleteMap(_mapId))
            .Wait();
    }
}
