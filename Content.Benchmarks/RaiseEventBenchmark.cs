#nullable enable
using System.IO;
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
public partial class RaiseEventBenchmark
{
    private TestPair _pair = default!;
    private BenchSystem _sys = default!;

    [GlobalSetup]
    public void Setup()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup(typeof(BenchSystem).Assembly);
        _pair = PoolManager.GetServerClient(testContext: new ExternalTestContext("Benchmark", StreamWriter.Null)).GetAwaiter().GetResult();
        var entMan = _pair.Server.EntMan;
        var fact = _pair.Server.ResolveDependency<IComponentFactory>();
        var bus = (EntityEventBus)entMan.EventBus;
        _sys = entMan.System<BenchSystem>();

        _pair.Server.WaitPost(() =>
        {
            var uid = entMan.Spawn();
            _sys.Ent = new(uid, entMan.GetComponent<TransformComponent>(uid));
            _sys.Ent2 = new(_sys.Ent.Owner, _sys.Ent.Comp);
            _sys.NetId = fact.GetRegistration<TransformComponent>().NetID!.Value;
            _sys.EvSubs = bus.GetNetCompEventHandlers<BenchSystem.BenchEv>();

            var id2 = entMan.Spawn();
            entMan.AddComponent<BenchSystem.Bench1Component>(id2);
            entMan.AddComponent<BenchSystem.Bench2Component>(id2);
            _sys.Ent2Comps = id2;

            var id4 = entMan.Spawn();
            entMan.AddComponent<BenchSystem.Bench1Component>(id4);
            entMan.AddComponent<BenchSystem.Bench2Component>(id4);
            entMan.AddComponent<BenchSystem.Bench3Component>(id4);
            entMan.AddComponent<BenchSystem.Bench4Component>(id4);
            _sys.Ent4Comps = id4;

            var id8 = entMan.Spawn();
            entMan.AddComponent<BenchSystem.Bench1Component>(id8);
            entMan.AddComponent<BenchSystem.Bench2Component>(id8);
            entMan.AddComponent<BenchSystem.Bench3Component>(id8);
            entMan.AddComponent<BenchSystem.Bench4Component>(id8);
            entMan.AddComponent<BenchSystem.Bench5Component>(id8);
            entMan.AddComponent<BenchSystem.Bench6Component>(id8);
            entMan.AddComponent<BenchSystem.Bench7Component>(id8);
            entMan.AddComponent<BenchSystem.Bench8Component>(id8);
            _sys.Ent8Comps = id8;

            var id16 = entMan.Spawn();
            entMan.AddComponent<BenchSystem.Bench1Component>(id16);
            entMan.AddComponent<BenchSystem.Bench2Component>(id16);
            entMan.AddComponent<BenchSystem.Bench3Component>(id16);
            entMan.AddComponent<BenchSystem.Bench4Component>(id16);
            entMan.AddComponent<BenchSystem.Bench5Component>(id16);
            entMan.AddComponent<BenchSystem.Bench6Component>(id16);
            entMan.AddComponent<BenchSystem.Bench7Component>(id16);
            entMan.AddComponent<BenchSystem.Bench8Component>(id16);
            entMan.AddComponent<BenchSystem.Bench9Component>(id16);
            entMan.AddComponent<BenchSystem.Bench10Component>(id16);
            entMan.AddComponent<BenchSystem.Bench11Component>(id16);
            entMan.AddComponent<BenchSystem.Bench12Component>(id16);
            entMan.AddComponent<BenchSystem.Bench13Component>(id16);
            entMan.AddComponent<BenchSystem.Bench14Component>(id16);
            entMan.AddComponent<BenchSystem.Bench15Component>(id16);
            entMan.AddComponent<BenchSystem.Bench16Component>(id16);
            _sys.Ent16Comps = id16;
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
    public int RaiseEvent1()
    {
        return _sys.RaiseEvent1();
    }

    [Benchmark]
    public int RaiseEvent2()
    {
        return _sys.RaiseEvent2();
    }

    [Benchmark]
    public int RaiseEvent4()
    {
        return _sys.RaiseEvent4();
    }

    [Benchmark]
    public int RaiseEvent8()
    {
        return _sys.RaiseEvent8();
    }

    [Benchmark]
    public int RaiseEvent16()
    {
        return _sys.RaiseEvent16();
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
    public int RaiseNetEvent()
    {
        return _sys.RaiseNetIdEvent();
    }

    [Benchmark]
    public int RaiseCSharpEvent()
    {
        return _sys.CSharpEvent();
    }

    public sealed partial class BenchSystem : EntitySystem
    {
        public Entity<TransformComponent> Ent;
        public Entity<IComponent> Ent2;
        public EntityUid Ent2Comps;
        public EntityUid Ent4Comps;
        public EntityUid Ent8Comps;
        public EntityUid Ent16Comps;

        public delegate void EntityEventHandler(EntityUid uid, TransformComponent comp, ref BenchEv ev);

        public event EntityEventHandler? OnCSharpEvent;
        public ushort NetId;
        internal EntityEventBus.DirectedEventHandler?[] EvSubs = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TransformComponent, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench1Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench2Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench3Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench4Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench5Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench6Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench7Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench8Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench9Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench10Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench11Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench12Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench13Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench14Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench15Component, BenchEv>(OnEvent);
            SubscribeLocalEvent<Bench16Component, BenchEv>(OnEvent);
            OnCSharpEvent += OnEvent;
        }

        public int RaiseEvent1()
        {
            var ev = new BenchEv();
            RaiseLocalEvent(Ent.Owner, ref ev);
            return ev.N;
        }

        public int RaiseEvent2()
        {
            var ev = new BenchEv();
            RaiseLocalEvent(Ent2Comps, ref ev);
            return ev.N;
        }

        public int RaiseEvent4()
        {
            var ev = new BenchEv();
            RaiseLocalEvent(Ent4Comps, ref ev);
            return ev.N;
        }

        public int RaiseEvent8()
        {
            var ev = new BenchEv();
            RaiseLocalEvent(Ent8Comps, ref ev);
            return ev.N;
        }

        public int RaiseEvent16()
        {
            var ev = new BenchEv();
            RaiseLocalEvent(Ent16Comps, ref ev);
            return ev.N;
        }

        public int RaiseCompEvent()
        {
            var ev = new BenchEv();
            RaiseComponentEvent(Ent.Owner, Ent.Comp, ref ev);
            return ev.N;
        }

        public int RaiseICompEvent()
        {
            // Raise with an IComponent instead of concrete type
            var ev = new BenchEv();
            RaiseComponentEvent(Ent2.Owner, Ent2.Comp, ref ev);
            return ev.N;
        }

        public int RaiseNetIdEvent()
        {
            // Raise a "IComponent" event using a net-id index delegate array (for PVS & client game-state events)
            var ev = new BenchEv();
            ref var unitEv = ref Unsafe.As<BenchEv, EntityEventBus.Unit>(ref ev);
            EvSubs[NetId]?.Invoke(Ent2.Owner, Ent2.Comp, ref unitEv);
            return ev.N;
        }

        public int CSharpEvent()
        {
            var ev = new BenchEv();
            OnCSharpEvent?.Invoke(Ent.Owner, Ent.Comp, ref ev);
            return ev.N;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnEvent<T>(EntityUid uid, T component, ref BenchEv args)
        {
            args.N += uid.Id;
        }

        [ByRefEvent]
        [ComponentEvent(Exclusive = false)]
        public struct BenchEv
        {
            public int N;
        }

        [RegisterComponent]
        public sealed partial class Bench1Component : Component;

        [RegisterComponent]
        public sealed partial class Bench2Component : Component;

        [RegisterComponent]
        public sealed partial class Bench3Component : Component;

        [RegisterComponent]
        public sealed partial class Bench4Component : Component;

        [RegisterComponent]
        public sealed partial class Bench5Component : Component;

        [RegisterComponent]
        public sealed partial class Bench6Component : Component;

        [RegisterComponent]
        public sealed partial class Bench7Component : Component;

        [RegisterComponent]
        public sealed partial class Bench8Component : Component;

        [RegisterComponent]
        public sealed partial class Bench9Component : Component;

        [RegisterComponent]
        public sealed partial class Bench10Component : Component;

        [RegisterComponent]
        public sealed partial class Bench11Component : Component;

        [RegisterComponent]
        public sealed partial class Bench12Component : Component;

        [RegisterComponent]
        public sealed partial class Bench13Component : Component;

        [RegisterComponent]
        public sealed partial class Bench14Component : Component;

        [RegisterComponent]
        public sealed partial class Bench15Component : Component;

        [RegisterComponent]
        public sealed partial class Bench16Component : Component;
    }
}
