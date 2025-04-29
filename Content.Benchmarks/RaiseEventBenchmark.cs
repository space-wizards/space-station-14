#nullable enable
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Benchmarks;

[Virtual]
public class RaiseEventBenchmark
{
    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private BenchSystem _sys = default!;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup(typeof(BenchSystem).Assembly);
        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        _entMan = _pair.Server.ResolveDependency<IEntityManager>();
        _sys = _entMan.System<BenchSystem>();
        _pair.Server.WaitPost(() => _sys.UidA = _entMan.Spawn()).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    [Benchmark]
    public int StructEvents()
    {
        return _sys.RaiseEvent();
    }
}

[ByRefEvent]
public struct BenchEv
{
    public int N;
}

public sealed class BenchSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformComponent, BenchEv>(OnEvent);
    }

    public EntityUid UidA;

    public int RaiseEvent()
    {
        var ev = new BenchEv();
        RaiseLocalEvent(UidA, ref ev);
        return ev.N;
    }

    private void OnEvent(EntityUid uid, TransformComponent component, ref BenchEv args)
    {
        args.N += uid.Id;
    }
}
