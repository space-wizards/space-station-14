using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Checks networking of temperature data inside GasTileOverlay
/// </summary>
public sealed partial class SharedGasTileOverlayTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");
    public override PoolSettings PoolSettings => new()
    {
        Connected = true
    };

    [SidedDependency(Side.Server)] private readonly SharedMapSystem _mapSys = default!;
    [SidedDependency(Side.Server)] private ItemToggleSystem _itemToggle = default!;

    [Test]
    [Description("Checks networking of temperature data inside GasTileOverlay.")]
    public async Task TestGasTileTemperatureOverlayDataSync()
    {
        var (gridCoords, tileIndices, mixture, cOverlay) = await PrepareGasTileTest();

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, 400f);

        await CheckForInjectedGas(cOverlay, tileIndices, 400f);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, 800f + ThermalByte.TempDegreeResolution - 1); // Rounding test

        await CheckForInjectedGas(cOverlay, tileIndices, 800f);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMaximum + 200f); // This one hits max temperature

        await CheckForInjectedGas(cOverlay, tileIndices, ThermalByte.TempMaximum);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMinimum);
        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMinimum + (ThermalByte.TempDegreeResolution * 2) - 1); // Test the networking optimisation, this should not be networked yet

        await CheckForInjectedGas(cOverlay, tileIndices, ThermalByte.TempMinimum);

        await InjectHotPlasma(ProcessEnt, tileIndices, mixture, ThermalByte.TempMinimum + (ThermalByte.TempDegreeResolution * 2)); // This should

        await CheckForInjectedGas(cOverlay, tileIndices, ThermalByte.TempMinimum + (ThermalByte.TempDegreeResolution * 2));
    }

    private async Task CheckForInjectedGas(GasTileOverlayComponent overlay, Vector2i indices, float expectedTemp)
    {
        await Client.WaitPost(() =>
        {
            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(indices);

            Assert.That(overlay.Chunks.TryGetValue(chunkIndices, out var chunk), "Chunk not found");
            Assert.That(chunk, Is.Not.Null, "Chunk not found");

            // Calculate the exact index in the TileData array
            var localX = MathHelper.Mod(indices.X, SharedGasTileOverlaySystem.ChunkSize);
            var localY = MathHelper.Mod(indices.Y, SharedGasTileOverlaySystem.ChunkSize);
            var tileIndex = localX + localY * SharedGasTileOverlaySystem.ChunkSize;

            var tile = chunk.TileData[tileIndex];
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
