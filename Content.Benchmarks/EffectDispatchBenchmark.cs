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
            var uid = entMan.Spawn();
            _sys.Target = new(uid, entMan.GetComponent<TransformComponent>(uid));
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
    public int EventBusDispatch()
    {
        return _sys.RaiseViaEventBus();
    }

    [Benchmark]
    public int StaticHandlerDispatch()
    {
        return _sys.RaiseViaStaticHandler();
    }

    [Benchmark]
    public int CSharpEventDispatch()
    {
        return _sys.RaiseViaCSharpEvent();
    }

    public sealed partial class BenchSystem : EntitySystem
    {
        private SharedEntityEffectsSystem _effectsSystem = default!;

        public Entity<TransformComponent> Target;

        private EntityEffect _effect = new TestEffect();

        private int _counter;

        public delegate void EffectHandler(EntityUid uid);
        public event EffectHandler OnEffect;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransformComponent, TestEffectEvent>(OnEventBus);

            OnEffect += OnCSharpEvent;

            _effectsSystem = EntityManager.System<SharedEntityEffectsSystem>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnEventBus(Entity<TransformComponent> entity, ref TestEffectEvent args)
        {
            _counter++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnCSharpEvent(EntityUid uid)
        {
            _counter++;
        }

        public int RaiseViaEventBus()
        {
            _counter = 0;
            var ev = new TestEffectEvent();
            RaiseLocalEvent(Target.Owner, ref ev);
            return _counter;
        }

        public int RaiseViaStaticHandler()
        {
            _counter = 0;
            _effectsSystem.ApplyEffect(Target, _effect);
            return _counter;
        }

        public int RaiseViaCSharpEvent()
        {
            _counter = 0;
            OnEffect?.Invoke(Target.Owner);
            return _counter;
        }
    }

    [ByRefEvent]
    public struct TestEffectEvent
    {
    }


    public sealed partial class TestEffect : EntityEffect
    {

    }

    public interface ITestHandler
    {
        void Handle(Entity<TransformComponent> target);
    }

    public sealed class TestHandler : ITestHandler
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Handle(Entity<TransformComponent> target)
        {
        }
    }
}
