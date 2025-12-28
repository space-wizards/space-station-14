using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Benchmarks;

/// <summary>
/// Benchmarks the performance of different gas reactions.
/// Tests each reaction type with realistic gas mixtures to measure computational cost.
/// </summary>
[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class GasReactionBenchmark
{
    private const int Iterations = 1000;

    /// <summary>
    /// Number of entities to spawn for tile processing benchmarks.
    /// Can be adjusted to test different density scenarios.
    /// </summary>
    [Params(1000)]
    public int EntityCount { get; set; }
    private TestPair _pair = default!;
    private AtmosphereSystem _atmosphereSystem = default!;

    // Grid and tile for reactions that need a holder
    private EntityUid _testGrid = default!;
    private TileAtmosphere _testTile = default!;
    // Reaction instances
    private PlasmaFireReaction _plasmaFireReaction = default!;
    private TritiumFireReaction _tritiumFireReaction = default!;
    private FrezonProductionReaction _frezonProductionReaction = default!;
    private FrezonCoolantReaction _frezonCoolantReaction = default!;
    private AmmoniaOxygenReaction _ammoniaOxygenReaction = default!;
    private N2ODecompositionReaction _n2oDecompositionReaction = default!;
    private WaterVaporReaction _waterVaporReaction = default!;
    private ElectrovaeProductionReaction _electrovaeProductionReaction = default!;
    private ElectrovaeChargeReaction _electrovaeChargeReaction = default!;
    private ChargedElectrovaeDischargeReaction _chargedElectrovaeDischargeReaction = default!;
    // Gas mixtures for each reaction type
    private GasMixture _plasmaFireMixture = default!;
    private GasMixture _tritiumFireMixture = default!;
    private GasMixture _frezonProductionMixture = default!;
    private GasMixture _frezonCoolantMixture = default!;
    private GasMixture _ammoniaOxygenMixture = default!;
    private GasMixture _n2oDecompositionMixture = default!;
    private GasMixture _waterVaporMixture = default!;
    private GasMixture _electrovaeProductionMixture = default!;
    private GasMixture _electrovaeChargeMixture = default!;
    private GasMixture _chargedElectrovaeDischargeM = default!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        // Create test map and grid
        var mapData = await _pair.CreateTestMap();
        _testGrid = mapData.Grid;

        await server.WaitPost(() =>
        {
            var entMan = server.ResolveDependency<IEntityManager>();
            _atmosphereSystem = entMan.System<AtmosphereSystem>();

            _plasmaFireReaction = new PlasmaFireReaction();
            _tritiumFireReaction = new TritiumFireReaction();
            _frezonProductionReaction = new FrezonProductionReaction();
            _frezonCoolantReaction = new FrezonCoolantReaction();
            _ammoniaOxygenReaction = new AmmoniaOxygenReaction();
            _n2oDecompositionReaction = new N2ODecompositionReaction();
            _waterVaporReaction = new WaterVaporReaction();
            _electrovaeProductionReaction = new ElectrovaeProductionReaction();
            _electrovaeChargeReaction = new ElectrovaeChargeReaction();
            _chargedElectrovaeDischargeReaction = new ChargedElectrovaeDischargeReaction();

            SetupGasMixtures();
            SetupTile();
        });
    }

    private void SetupGasMixtures()
    {
        // Plasma Fire: Plasma + Oxygen at high temperature
        // Temperature must be > PlasmaMinimumBurnTemperature for reaction to occur
        _plasmaFireMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.PlasmaMinimumBurnTemperature + 100f // ~673K
        };
        _plasmaFireMixture.AdjustMoles(Gas.Plasma, 20f);
        _plasmaFireMixture.AdjustMoles(Gas.Oxygen, 100f);

        // Tritium Fire: Tritium + Oxygen at high temperature
        // Temperature must be > FireMinimumTemperatureToExist for reaction to occur
        _tritiumFireMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.FireMinimumTemperatureToExist + 100f // ~473K
        };
        _tritiumFireMixture.AdjustMoles(Gas.Tritium, 20f);
        _tritiumFireMixture.AdjustMoles(Gas.Oxygen, 100f);

        // Frezon Production: Oxygen + Tritium + Nitrogen catalyst
        // Optimal temperature for efficiency (80% of max efficiency temp)
        _frezonProductionMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.FrezonProductionMaxEfficiencyTemperature * 0.8f // ~48K
        };
        _frezonProductionMixture.AdjustMoles(Gas.Oxygen, 50f);
        _frezonProductionMixture.AdjustMoles(Gas.Tritium, 50f);
        _frezonProductionMixture.AdjustMoles(Gas.Nitrogen, 10f);

        // Frezon Coolant: Frezon + Nitrogen
        // Temperature must be > FrezonCoolLowerTemperature (23.15K) for reaction to occur
        _frezonCoolantMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C + 50f // ~343K
        };
        _frezonCoolantMixture.AdjustMoles(Gas.Frezon, 30f);
        _frezonCoolantMixture.AdjustMoles(Gas.Nitrogen, 100f);

        // Ammonia + Oxygen reaction (concentration-dependent, no temp requirement)
        _ammoniaOxygenMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C + 100f // ~393K
        };
        _ammoniaOxygenMixture.AdjustMoles(Gas.Ammonia, 40f);
        _ammoniaOxygenMixture.AdjustMoles(Gas.Oxygen, 40f);

        // N2O Decomposition (no temperature requirement, just needs N2O moles)
        _n2oDecompositionMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C + 100f // ~393K
        };
        _n2oDecompositionMixture.AdjustMoles(Gas.NitrousOxide, 100f);

        // Water Vapor - needs water vapor to condense
        _waterVaporMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C
        };
        _waterVaporMixture.AdjustMoles(Gas.WaterVapor, 50f);

        // Electrovae Production: N2O + H2O -> Electrovae + O2
        // Minimum temp: 373.15K (100°C), Full efficiency: 1370K (1097°C)
        _electrovaeProductionMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = 1370f // Full efficiency temperature
        };
        _electrovaeProductionMixture.AdjustMoles(Gas.WaterVapor, 50f);
        _electrovaeProductionMixture.AdjustMoles(Gas.NitrousOxide, 50f);
        _electrovaeProductionMixture.AdjustMoles(Gas.Nitrogen, 10f); // Catalyst

        // Electrovae Charging: Electrovae + Heat -> ChargedElectrovae
        // Minimum temp: 1508K (1235°C), Full efficiency: 1984K (1711°C)
        _electrovaeChargeMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = 1984f // Full efficiency temperature
        };
        _electrovaeChargeMixture.AdjustMoles(Gas.Electrovae, 100f);

        // Charged Electrovae Discharge: ChargedElectrovae + O2 -> Electrovae (slow conversion)
        // Provides power to nearby machines and batteries
        _chargedElectrovaeDischargeM = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C
        };
        _chargedElectrovaeDischargeM.AdjustMoles(Gas.ChargedElectrovae, 100f);
        _chargedElectrovaeDischargeM.AdjustMoles(Gas.Oxygen, 10f); // Needs oxygen to discharge

        // Setup tile processing benchmark entities
        var entMan = _pair.Server.ResolveDependency<IEntityManager>();

        // Create a tile with charged electrovae gas
        var testIndices = new Vector2i(5, 5);
        _tileProcessingTile = new TileAtmosphere(_testGrid, testIndices, new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C
        });

        // Add charged electrovae gas (2 moles = full intensity)
        _tileProcessingTile.Air!.AdjustMoles(Gas.ChargedElectrovae, 2f);
        _tileProcessingTile.Air.AdjustMoles(Gas.Oxygen, 10f);

        _tileProcessingEntities = [];

        // Spawn mix of entities based on EntityCount parameter
        // Distribution: 20% batteries, 60% machines, 20% mobs
        var batteryCount = (int)(EntityCount * 0.2f);
        var machineCount = (int)(EntityCount * 0.6f);
        var mobCount = EntityCount - batteryCount - machineCount;

        var tileCenter = new Vector2(testIndices.X + 0.5f, testIndices.Y + 0.5f);

        // Spawn batteries (SMES, APCs)
        for (var i = 0; i < batteryCount; i++)
        {
            var ent = entMan.SpawnEntity("SMESBasic", new EntityCoordinates(_testGrid, tileCenter));
            _tileProcessingEntities.Add(ent);
        }

        // Spawn machines with power receivers (computers, consoles, etc.)
        for (var i = 0; i < machineCount; i++)
        {
            var ent = entMan.SpawnEntity("ComputerAlert", new EntityCoordinates(_testGrid, tileCenter));
            _tileProcessingEntities.Add(ent);
        }

        // Spawn mobs (for lightning strike testing)
        for (var i = 0; i < mobCount; i++)
        {
            var ent = entMan.SpawnEntity("MobHuman", new EntityCoordinates(_testGrid, tileCenter));
            _tileProcessingEntities.Add(ent);
        }
    }

    private void SetupTile()
    {
        // Create a tile atmosphere to use as holder for all reactions
        var testIndices = new Vector2i(0, 0);
        _testTile = new TileAtmosphere(_testGrid, testIndices, new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C
        });
    }

    private static GasMixture CloneMixture(GasMixture original)
    {
        return new GasMixture(original);
    }

    [Benchmark]
    public async Task PlasmaFireReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_plasmaFireMixture);
                _plasmaFireReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task TritiumFireReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_tritiumFireMixture);
                _tritiumFireReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task FrezonProductionReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_frezonProductionMixture);
                _frezonProductionReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task FrezonCoolantReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_frezonCoolantMixture);
                _frezonCoolantReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task AmmoniaOxygenReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_ammoniaOxygenMixture);
                _ammoniaOxygenReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task N2ODecompositionReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_n2oDecompositionMixture);
                _n2oDecompositionReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task WaterVaporReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_waterVaporMixture);
                _waterVaporReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task ElectrovaeProductionReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_electrovaeProductionMixture);
                _electrovaeProductionReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task ElectrovaeChargeReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_electrovaeChargeMixture);
                _electrovaeChargeReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task ChargedElectrovaeDischargeReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_chargedElectrovaeDischargeM);
                _chargedElectrovaeDischargeReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    private List<EntityUid> _tileProcessingEntities = default!;
    private TileAtmosphere _tileProcessingTile = default!;

    /// <summary>
    /// Benchmarks the real-world performance of charged electrovae discharge with realistic entity counts.
    /// Tests the tile-based processing system including entity iteration, battery charging,
    /// capacity expansion, power toggling, and lightning strikes.
    /// This simulates the actual atmos tick processing cost.
    /// </summary>
    [Benchmark]
    public async Task ProcessChargedElectrovaeTile()
    {
        await _pair.Server.WaitPost(() =>
        {
            // Run the discharge reaction which triggers tile processing
            for (var i = 0; i < Iterations; i++)
            {
                // Clone the mixture to reset state each iteration
                var mixture = CloneMixture(_tileProcessingTile.Air);

                // The discharge reaction internally calls ChargedElectrovaeExpose
                // which marks the tile and triggers entity processing
                _chargedElectrovaeDischargeReaction.React(mixture, _tileProcessingTile, _atmosphereSystem, 1f);
            }
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.Server.WaitPost(() =>
        {
            var entMan = _pair.Server.ResolveDependency<IEntityManager>();
            foreach (var ent in _tileProcessingEntities)
            {
                entMan.DeleteEntity(ent);
            }
        });

        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
