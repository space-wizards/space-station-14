using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Numerics;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// GasTileOverlay is being tested here
/// </summary>
public sealed class GasTileOverlayTemperatureNetworkingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");
    public override PoolSettings PoolSettings => new()
    {
        Connected = true
    };

    [SidedDependency(Side.Server)] private readonly SharedMapSystem _mapSys = default!;

    [Test]
    public async Task TestGasOverlayDataSync()
    {
        await Server.WaitPost(delegate
        {
            // funny thing, this grid is a star so we need to spawn some ents to give us one cell
            // otherwise the gas will spread to other areas and itll be weird
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var offsetOrigin = Vector2i.Zero.Offset(direction);
                SSpawnAtPosition("WallSolid", new EntityCoordinates(ProcessEnt, offsetOrigin));
            }
        });

        await RunUntilSynced();

        var gridComp = ProcessEnt.Comp2;
        var gridNetEnt = Server.EntMan.GetNetEntity(ProcessEnt);

        var gridCoords = new EntityCoordinates(ProcessEnt, Vector2.Zero);
        var tileIndices = _mapSys.TileIndicesFor(ProcessEnt, gridComp, gridCoords);
        var mixture = SAtmos.GetTileMixture(ProcessEnt, null, tileIndices, true);

        var cGridEnt = CEntMan.GetEntity(gridNetEnt);

        // Check if the server actually sent the gas chunks
        await RunUntilSynced();

        // Start real tests
        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, 400f);

        await CheckForInjectedGas(cGridEnt, tileIndices, 400f);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, 800f + ThermalByte.TempDegreeResolution - 1); // Rounding test

        await CheckForInjectedGas(cGridEnt, tileIndices, 800f);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMaximum + 200f); // This one hits max temperature

        await CheckForInjectedGas(cGridEnt, tileIndices, ThermalByte.TempMaximum);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMinimum);
        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMinimum + (ThermalByte.TempDegreeResolution * 2) - 1); // Test the networking optimisation, this should not be networked yet

        await CheckForInjectedGas(cGridEnt, tileIndices, ThermalByte.TempMinimum);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMinimum + (ThermalByte.TempDegreeResolution * 2)); // This should

        await CheckForInjectedGas(cGridEnt, tileIndices, ThermalByte.TempMinimum + (ThermalByte.TempDegreeResolution * 2));
    }

    private async Task CheckForInjectedGas(EntityUid grid, Vector2i indices, float expectedTemp)
    {
        await Client.WaitPost(() =>
        {
            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(indices);
            var chunks = CEntMan.System<ChunkEntitySystem>();

            Assert.That(chunks.TryGetChunk(grid, chunkIndices, out var chunkEnt), "Chunk not found");
            Assert.That(CTryComp<GasOverlayChunkComponent>(chunkEnt!.Value.Owner, out var chunk), "Chunk overlay data not found");

            // Calculate the exact index in the chunk data arrays.
            var localX = MathHelper.Mod(indices.X, SharedGasTileOverlaySystem.ChunkSize);
            var localY = MathHelper.Mod(indices.Y, SharedGasTileOverlaySystem.ChunkSize);
            var tileIndex = localX + localY * SharedGasTileOverlaySystem.ChunkSize;

            var tile = chunk.TemperatureData[tileIndex];
            Assert.That(tile.Active, Is.True, "Tile had no temperature overlay data!");
            tile.ByteGasTemperature.TryGetTemperature(out var actualTemp);

            Assert.That(actualTemp, Is.EqualTo(expectedTemp).Within(0.01f), $"Tile at {indices} had wrong temperature!");
        });
    }

    private async Task InjectHotPlasma(EntityUid gridEnt, Vector2i tileIndices, GasMixture mixture, float temperature)
    {
        //Server makes atmos
        await Server.WaitPost(() =>
        {
            if (mixture != null)
            {
                mixture.Clear();
                mixture.AdjustMoles(Gas.Plasma, 100f); // Inject hot plasma
                mixture.Temperature = temperature;
                SAtmos.InvalidateVisuals(gridEnt, tileIndices);
            }
        });
        await Server.WaitRunTicks(10);
        await RunUntilSynced();
    }
}
