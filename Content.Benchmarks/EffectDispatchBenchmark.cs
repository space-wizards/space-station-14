using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Shared.EntityEffects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Benchmarks;

/// <summary>
/// Benchmark comparing EntityEffect dispatch strategies.
/// Old: event bus SubscribeLocalEvent + RaiseLocalEvent
/// New: static Dictionary{{Type, IEntityEffectHandler}} + direct interface call
/// </summary>
[Virtual]
public partial class EffectDispatchBenchmark
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
        _sys = entMan.System<BenchSystem>();

        _pair.Server.WaitPost(() =>
        {
            var hitUid = entMan.Spawn();
            entMan.AddComponent<DummyComponent>(hitUid);
            _sys.HitTarget = new(hitUid, entMan.GetComponent<TransformComponent>(hitUid));

            var missUid = entMan.Spawn();
            _sys.MissTarget = new(missUid, entMan.GetComponent<TransformComponent>(missUid));

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
    public int EventBusDispatch_Hit()
    {
        return _sys.RaiseViaEventBusHit();
    }

    [Benchmark]
    public int EventBusDispatch_Miss()
    {
        return _sys.RaiseViaEventBusMiss();
    }

    [Benchmark]
    public int CurrentImplementation_Hit()
    {
        return _sys.RaiseViaCurrentImplementation();
    }

    [Benchmark]
    public int CurrentImplementation_Miss()
    {
        return _sys.RaiseViaCurrentImplementationMiss();
    }

    [Benchmark]
    public int CSharpEventDispatch_Hit()
    {
        return _sys.RaiseViaCSharpEventHit();
    }

    [Benchmark]
    public int CSharpEventDispatch_Miss()
    {
        return _sys.RaiseViaCSharpEventHit();
    }

    public sealed partial class BenchSystem : EntitySystem
    {
        private SharedEntityEffectsSystem _effectsSystem = default!;

        public Entity<TransformComponent> HitTarget;
        public Entity<TransformComponent> MissTarget;

        private EntityEffect _effect = new TestEffect();

        private int _counter;

        public delegate void EffectHandler(EntityUid uid);

        public event EffectHandler OnEffect;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DummyComponent, TestEffectEvent>(OnEventBus);

            OnEffect += OnCSharpEvent;

            _effectsSystem = EntityManager.System<SharedEntityEffectsSystem>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnEventBus(Entity<DummyComponent> entity, ref TestEffectEvent args)
        {
            _counter++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnCSharpEvent(EntityUid uid)
        {
            TryComp<DummyComponent>(uid, out _);
            _counter++;
        }

        public int RaiseViaEventBusHit()
        {
            _counter = 0;
            var ev = new TestEffectEvent();
            RaiseLocalEvent(HitTarget.Owner, ref ev);
            return _counter;
        }

        public int RaiseViaEventBusMiss()
        {
            _counter = 0;
            var ev = new TestEffectEvent();
            RaiseLocalEvent(MissTarget.Owner, ref ev);
            return _counter;
        }

        public int RaiseViaCurrentImplementation()
        {
            _counter = 0;
            _effectsSystem.ApplyEffect(HitTarget, _effect);
            return _counter;
        }

        public int RaiseViaCurrentImplementationMiss()
        {
            _counter = 0;
            _effectsSystem.ApplyEffect(MissTarget, _effect);
            return _counter;
        }

        public int RaiseViaCSharpEventHit()
        {
            _counter = 0;
            OnEffect?.Invoke(HitTarget.Owner);
            return _counter;
        }

        public int RaiseViaCSharpEventMiss()
        {
            _counter = 0;
            OnEffect?.Invoke(MissTarget.Owner);
            return _counter;
        }
    }

    [ByRefEvent]
    public struct TestEffectEvent
    {
    }

    public sealed partial class TestEffectSystem : EntityEffectSystem<DummyComponent, TestEffect>
    {
        protected override void Effect(Entity<DummyComponent> entity, ref EntityEffectEvent<TestEffect> args)
        {
        }
    }
    public sealed partial class TestEffect : EntityEffectBase<TestEffect>
    {

    }

    [RegisterComponent]
    public sealed partial class DummyComponent : Component
    {
    }
}
