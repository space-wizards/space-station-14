using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.Server.Maps;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Benchmarks;

[Virtual]
public class MapLoadBenchmark
{
    private PairTracker _pair = default!;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        Paths = _pair.Pair.Server.ResolveDependency<IPrototypeManager>()
            .EnumeratePrototypes<GameMapPrototype>().ToDictionary(x => x.ID, x => x.MapPath.ToString());
        IoCManager.InitThread(_pair.Pair.Server.InstanceDependencyCollection);
    }

    public static IEnumerable<string> MapsSource { get; set; }

    [ParamsSource(nameof(MapsSource))] public string Map;

    public static Dictionary<string, string> Paths;

    [Benchmark]
    public void LoadMap()
    {
        _pair.Pair.Server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>().LoadMap(new MapId(10), Paths[Map]);
    }
}
