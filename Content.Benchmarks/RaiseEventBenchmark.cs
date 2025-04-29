#nullable enable
using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

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
        _pair.Server.WaitPost(() =>
        {
            var uid = _entMan.Spawn();
            _sys.Ent = new(uid, _entMan.GetComponent<TransformComponent>(uid));
        })
            .GetAwaiter()
            .GetResult();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }

    [Benchmark(Baseline = true)]
    public int RaiseEvent()
    {
        return _sys.RaiseEvent();
    }

    [Benchmark]
    public int RaiseCompEvent()
    {
        return _sys.RaiseCompEvent();
    }

    [Benchmark]
    public int RaiseCSharpEvent()
    {
        return _sys.CSharpEvent();
    }

    [Benchmark]
    public int DirectInvoke()
    {
        return _sys.Direct();
    }

    public sealed class BenchSystem : EntitySystem
    {
        public Entity<TransformComponent> Ent;

        public delegate void EntityEventHandler(EntityUid uid, TransformComponent comp, ref BenchEv ev);

        public event EntityEventHandler? OnCSharpEvent;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TransformComponent, BenchEv>(OnEvent);
            OnCSharpEvent += OnEvent;
        }

        public int RaiseEvent()
        {
            var ev = new BenchEv();
            RaiseLocalEvent(Ent.Owner, ref ev);
            return ev.N;
        }

        public int RaiseCompEvent()
        {
            var ev = new BenchEv();
            EntityManager.EventBus.RaiseComponentEvent(Ent.Owner, Ent.Comp, ref ev);
            return ev.N;
        }

        public int CSharpEvent()
        {
            var ev = new BenchEv();
            OnCSharpEvent?.Invoke(Ent.Owner, Ent.Comp, ref ev);
            return ev.N;
        }

        public int Direct()
        {
            var ev = new BenchEv();
            OnEvent(Ent.Owner, Ent.Comp, ref ev);
            return ev.N;
        }

        private void OnEvent(EntityUid uid, TransformComponent component, ref BenchEv args)
        {
            args.N += uid.Id;
        }

        [ByRefEvent]
        public struct BenchEv
        {
            public int N;
        }
    }
}
