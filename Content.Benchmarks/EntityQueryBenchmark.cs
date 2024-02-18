#nullable enable
using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Benchmarks;

[Virtual]
public class EntityQueryBenchmark
{
    public const string Map = "Maps/atlas.yml";

    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private MapId _mapId = new MapId(10);
    private EntityQuery<ClothingComponent> _clothingQuery;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup(null);

        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        _entMan = _pair.Server.ResolveDependency<IEntityManager>();

        _pair.Server.ResolveDependency<IRobustRandom>().SetSeed(42);
        _pair.Server.WaitPost(() =>
        {
            var success = _entMan.System<MapLoaderSystem>().TryLoad(_mapId, Map, out _);
            if (!success)
                throw new Exception("Map load failed");
            _pair.Server.MapMan.DoMapInitialize(_mapId);
        }).GetAwaiter().GetResult();

        _clothingQuery = _entMan.GetEntityQuery<ClothingComponent>();

        // Apparently ~40% of entities are items, and 1 in 6 of those are clothing.
        /*
        var entCount = _entMan.EntityCount;
        var itemCount = _entMan.Count<ItemComponent>();
        var clothingCount = _entMan.Count<ClothingComponent>();
        var itemRatio = (float) itemCount / entCount;
        var clothingRatio = (float) clothingCount / entCount;
        Console.WriteLine($"Entities: {entCount}. Items: {itemRatio:P2}. Clothing: {clothingRatio:P2}.");
        */
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    [Benchmark]
    public int HasComponent()
    {
        var hashCode = 0;
        var enumerator = _entMan.AllEntityQueryEnumerator<ItemComponent>();
        while (enumerator.MoveNext(out var uid, out var _))
        {
            if (_entMan.HasComponent<ClothingComponent>(uid))
                hashCode = HashCode.Combine(hashCode, uid.Id);
        }

        return hashCode;
    }

    [Benchmark]
    public int HasComponentQuery()
    {
        var hashCode = 0;
        var enumerator = _entMan.AllEntityQueryEnumerator<ItemComponent>();
        while (enumerator.MoveNext(out var uid, out var _))
        {
            if (_clothingQuery.HasComponent(uid))
                hashCode = HashCode.Combine(hashCode, uid.Id);
        }

        return hashCode;
    }

    [Benchmark]
    public int TryGetComponent()
    {
        var hashCode = 0;
        var enumerator = _entMan.AllEntityQueryEnumerator<ItemComponent>();
        while (enumerator.MoveNext(out var uid, out var _))
        {
            if (_entMan.TryGetComponent(uid, out ClothingComponent? clothing))
                hashCode = HashCode.Combine(hashCode, clothing.GetHashCode());
        }

        return hashCode;
    }

    [Benchmark]
    public int TryGetComponentQuery()
    {
        var hashCode = 0;
        var enumerator = _entMan.AllEntityQueryEnumerator<ItemComponent>();
        while (enumerator.MoveNext(out var uid, out var _))
        {
            if (_clothingQuery.TryGetComponent(uid, out var clothing))
                hashCode = HashCode.Combine(hashCode, clothing.GetHashCode());
        }

        return hashCode;
    }

    /// <summary>
    /// Enumerate all entities with both an item and clothing component.
    /// </summary>
    [Benchmark]
    public int Enumerator()
    {
        var hashCode = 0;
        var enumerator = _entMan.AllEntityQueryEnumerator<ItemComponent, ClothingComponent>();
        while (enumerator.MoveNext(out var _, out var clothing))
        {
            hashCode = HashCode.Combine(hashCode, clothing.GetHashCode());
        }

        return hashCode;
    }
}
