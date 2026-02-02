using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// GasTileOverlay is being tested here
/// </summary>
public sealed class GasTileOverlayTemperatureNetworkingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    [Test]
    public async Task TestGasOverlayDataSync()
    {
        var sMapManager = Server.ResolveDependency<IMapManager>();

        var sOverlay = Server.System<GasTileOverlaySystem>();
        var sMapSys = Server.System<SharedMapSystem>();

        NetEntity gridNetEnt = default;

        var mapId = sMapSys.GetAllMapIds().First(m => m != MapId.Nullspace);

        var gridComp = sMapManager.GetAllMapGrids(mapId).First();
        var gridEnt = gridComp.Owner;
        gridNetEnt = Server.EntMan.GetNetEntity(gridEnt);

        Server.EntMan.EnsureComponent<GasTileOverlayComponent>(gridEnt);

        var gridCoords = new EntityCoordinates(gridEnt, Vector2.Zero);

        var tileIndices = sMapSys.TileIndicesFor(gridEnt, gridComp, gridCoords);
        var mixture = SAtmos.GetTileMixture(gridEnt, null, tileIndices, true);

        // Get data for client side.
        var cGridEnt = CEntMan.GetEntity(gridNetEnt);
        Assert.That(CEntMan.TryGetComponent<GasTileOverlayComponent>(cGridEnt, out var overlay),
            "Client grid is missing GasTileOverlayComponent");

        // Check if the server actually sent the gas chunks
        Assert.That(overlay.Chunks, Is.Not.Empty, "Gas overlay chunks are empty on the client.");

        //Start real tests
        await InjectHotPlasma(sOverlay, gridEnt, tileIndices, mixture, 400f);

        await CheckForInjectedGas(overlay, tileIndices, 400f);

        await InjectHotPlasma(sOverlay, gridEnt, tileIndices, mixture, 803f); // Rounding test

        await CheckForInjectedGas(overlay, tileIndices, 800f);

        await InjectHotPlasma(sOverlay, gridEnt, tileIndices, mixture, 1200f); // This one hits max temperature

        await CheckForInjectedGas(overlay, tileIndices, ThermalByte.TempMaximum);

        await InjectHotPlasma(sOverlay, gridEnt, tileIndices, mixture, 0);
        await InjectHotPlasma(sOverlay, gridEnt, tileIndices, mixture, 7); // Test the networking optimisation, this should not be networked yet 

        await CheckForInjectedGas(overlay, tileIndices, 0);

        await InjectHotPlasma(sOverlay, gridEnt, tileIndices, mixture, 10); // This should

        await CheckForInjectedGas(overlay, tileIndices, 8); // 10 is rounded down to 8
    }

    private async Task CheckForInjectedGas(GasTileOverlayComponent overlay, Vector2i indices, float expectedTemp)
    {
        await Client.WaitPost(() =>
        {
            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(indices);
            Assert.That(overlay.Chunks.TryGetValue(chunkIndices, out var chunk), "Chunk not found");

            // Calculate the exact index in the TileData array
            var localX = MathHelper.Mod(indices.X, SharedGasTileOverlaySystem.ChunkSize);
            var localY = MathHelper.Mod(indices.Y, SharedGasTileOverlaySystem.ChunkSize);
            int tileIndex = localX + localY * SharedGasTileOverlaySystem.ChunkSize;

            var tile = chunk.TileData[tileIndex];
            tile.ByteGasTemperature.TryGetTemperature(out var actualTemp);

            Assert.That(actualTemp == expectedTemp, $"Tile at {indices} had wrong temperature!");
        });
    }

    private async Task InjectHotPlasma(GasTileOverlaySystem sOverlay, EntityUid gridEnt, Vector2i tileIndices, GasMixture mixture, float temperature)
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

        await RunTicks(10);
        await Task.WhenAll(Client.WaitIdleAsync(), Server.WaitIdleAsync());
    }
}
