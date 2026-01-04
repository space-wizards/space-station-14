using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Benchmarks;

[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class HeatCapacityBenchmark
{
    private TestPair _pair = default!;
    private IEntityManager _sEntMan = default!;
    private IEntityManager _cEntMan = default!;
    private Client.Atmos.EntitySystems.AtmosphereSystem _cAtmos = default!;
    private AtmosphereSystem _sAtmos = default!;
    private GasMixture _mix;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        await _pair.Connect();
        _cEntMan = _pair.Client.ResolveDependency<IEntityManager>();
        _sEntMan = _pair.Server.ResolveDependency<IEntityManager>();
        _cAtmos = _cEntMan.System<Client.Atmos.EntitySystems.AtmosphereSystem>();
        _sAtmos = _sEntMan.System<AtmosphereSystem>();

        const float volume = 2500f;
        const float temperature = 293.15f;

        const float o2 = 12.3f;
        const float n2 = 45.6f;
        const float co2 = 0.42f;
        const float plasma = 0.05f;

        _mix = new GasMixture(volume) { Temperature = temperature };

        _mix.AdjustMoles(Gas.Oxygen, o2);
        _mix.AdjustMoles(Gas.Nitrogen, n2);
        _mix.AdjustMoles(Gas.CarbonDioxide, co2);
        _mix.AdjustMoles(Gas.Plasma, plasma);
    }

    [Benchmark]
    public async Task ClientHeatCapacityBenchmark()
    {
        await _pair.Client.WaitPost(delegate
        {
            for (var i = 0; i < 10000; i++)
            {
                _cAtmos.GetHeatCapacity(_mix, applyScaling: true);
            }
        });
    }

    [Benchmark]
    public async Task ServerHeatCapacityBenchmark()
    {
        await _pair.Server.WaitPost(delegate
        {
            for (var i = 0; i < 10000; i++)
            {
                _sAtmos.GetHeatCapacity(_mix, applyScaling: true);
            }
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
