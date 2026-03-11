using Content.Server.Light.EntitySystems;
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
/// GasTileOverlay is being tested here for visible gases networking.
/// </summary>
public sealed class SharedGasTileVisibleGasOverlaySystemTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    [Test]
    public async Task TestGasTileVisibleGasOverlayDataSync()
    {
        var sMapSys = Server.System<SharedMapSystem>();

        var gridComp = ProcessEnt.Comp3;
        var gridNetEnt = Server.EntMan.GetNetEntity(ProcessEnt);

        var gridCoords = new EntityCoordinates(ProcessEnt, Vector2.Zero);
        var tileIndices = sMapSys.TileIndicesFor(ProcessEnt, gridComp, gridCoords);
        var mixture = SAtmos.GetTileMixture(ProcessEnt, null, tileIndices, true);

        // Get data for client side.
        var cGridEnt = CEntMan.GetEntity(gridNetEnt);
        Assert.That(CEntMan.TryGetComponent<GasTileOverlayComponent>(cGridEnt, out var cOverlay),
            "Client grid is missing GasTileOverlayComponent");

        // Check if the server actually sent the gas chunks
        Assert.That(cOverlay, Is.Not.Null, "Gas overlay is null on the client.");
        Assert.That(cOverlay.Chunks, Is.Not.Empty, "Gas overlay chunks are empty on the client.");

        //Start real tests
        await Server.WaitPost(() =>
        {
            Assert.That(mixture, Is.Not.Null, "The gas mixture was not initialized.");
            mixture.Clear();
            mixture.AdjustMoles(Gas.WaterVapor, 100f);
            mixture.AdjustMoles(Gas.Oxygen, 100f);
        });

        await RunTicks(10);
        await Task.WhenAll(Client.WaitIdleAsync(), Server.WaitIdleAsync());


        await Client.WaitPost(() =>
        {
            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(tileIndices);

            Assert.That(cOverlay.Chunks.TryGetValue(chunkIndices, out var chunk), "Chunk not found");
            Assert.That(chunk, Is.Not.Null, "Chunk not found");

            // Calculate the exact index in the TileData array
            var localX = MathHelper.Mod(tileIndices.X, SharedGasTileOverlaySystem.ChunkSize);
            var localY = MathHelper.Mod(tileIndices.Y, SharedGasTileOverlaySystem.ChunkSize);
            int tileIndex = localX + localY * SharedGasTileOverlaySystem.ChunkSize;

            var tile = chunk.TileData[tileIndex];

            Assert.That(tile.Opacity.Count(b => b > 0), Is.EqualTo(1), $"Tile at {tileIndices} should have exactly one non-zero opacity value");
        });
    }
}
