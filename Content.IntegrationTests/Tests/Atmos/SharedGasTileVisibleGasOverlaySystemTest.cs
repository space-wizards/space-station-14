using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Maths;
using System.Linq;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Checks networking of visible gasses inside GasTileOverlay.
/// </summary>
public sealed partial class SharedGasTileOverlayTest
{
    [Test]
    [Description("Checks networking of visible gasses inside GasTileOverlay.")]
    public async Task TestGasTileVisibleGasOverlayDataSync()
    {
        var (gridCoords, tileIndices, mixture, cOverlay) = await PrepareGasTileTest();

        //Start real tests
        await Server.WaitPost(() =>
        {
            Assert.That(mixture, Is.Not.Null, "The gas mixture was not initialized.");
            mixture.Clear();
            mixture.AdjustMoles(Gas.WaterVapor, 100f);
            mixture.AdjustMoles(Gas.Oxygen, 100f);
        });

        await RunUntilSynced();
        await Pair.RunTicksSync(10);

        await Client.WaitPost(() =>
        {
            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(tileIndices);

            Assert.That(cOverlay.Chunks.TryGetValue(chunkIndices, out var chunk), "Chunk not found");
            Assert.That(chunk, Is.Not.Null, "Chunk not found");

            // Calculate the exact index in the TileData array
            var localX = MathHelper.Mod(tileIndices.X, SharedGasTileOverlaySystem.ChunkSize);
            var localY = MathHelper.Mod(tileIndices.Y, SharedGasTileOverlaySystem.ChunkSize);
            int tileIndex = localX + localY * SharedGasTileOverlaySystem.ChunkSize;

            var tile = chunk.TileVisibleGasData[tileIndex];

            Assert.That(tile.Opacity.Count(b => b > 0), Is.EqualTo(1), $"Tile at {tileIndices} should have exactly one non-zero opacity value");
        });
    }
}
