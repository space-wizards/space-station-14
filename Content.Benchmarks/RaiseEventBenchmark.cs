#nullable enable
using System.Runtime.CompilerServices;
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
    private BenchSystem _sys = default!;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup(typeof(BenchSystem).Assembly);
        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        var entMan = _pair.Server.EntMan;
        _sys = entMan.System<BenchSystem>();

        _pair.Server.WaitPost(() =>
        {
            var uid = entMan.Spawn();
            _sys.Ent = new(uid, entMan.GetComponent<TransformComponent>(uid));
            _sys.Ent2 = new(_sys.Ent.Owner, _sys.Ent.Comp);
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
    public int RaiseICompEvent()
    {
        return _sys.RaiseICompEvent();
    }

    [Benchmark]
    public int RaiseCSharpEvent()
    {
        return _sys.CSharpEvent();
    }

    public sealed class BenchSystem : EntitySystem
    {
        public Entity<TransformComponent> Ent;
        public Entity<IComponent> Ent2;

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

        public int RaiseICompEvent()
        {
            // Raise with an IComponent instead of concrete type
            var ev = new BenchEv();
            EntityManager.EventBus.RaiseComponentEvent(Ent2.Owner, Ent2.Comp, ref ev);
            return ev.N;
        }

        public int CSharpEvent()
        {
            var ev = new BenchEv();
            OnCSharpEvent?.Invoke(Ent.Owner, Ent.Comp, ref ev);
            return ev.N;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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
