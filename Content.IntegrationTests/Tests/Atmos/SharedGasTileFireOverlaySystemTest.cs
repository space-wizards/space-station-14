using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Checks networking of fire visuals inside GasTileOverlay
/// </summary>
public sealed partial class SharedGasTileOverlayTest : AtmosTest
{
    [Test]
    [Description("Checks networking of fire visuals inside GasTileOverlay.")]
    public async Task TestGasTileFireOverlayDataSync()
    {
        var (gridCoords, tileIndices, mixture, cOverlay) = await PrepareGasTileTest();

        //Start real tests
        await Server.WaitPost(() =>
        {
            Assert.That(mixture, Is.Not.Null, "The gas mixture was not initialized.");
            mixture.Clear();
            mixture.AdjustMoles(Gas.Plasma, 100f);
            mixture.AdjustMoles(Gas.Oxygen, 100f); // Inject flamable gasses

            var welder = SEntMan.SpawnEntity("Welder", gridCoords); // spawn welder
            Assert.That(ItemToggle.TryActivate(welder)); //ignite it which will set the tile on fire, this test dubles as for ignition of flammable gasses
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

            var tile = chunk.TileData[tileIndex];

            Assert.That(tile.FireState, Is.GreaterThan(0), $"Tile at {tileIndices} is not set on fire!");
        });
    }

    private async Task<(EntityCoordinates gridCoords, Vector2i tileIndices, GasMixture mixture, GasTileOverlayComponent cOverlay)> PrepareGasTileTest()
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
        var gridComp = ProcessEnt.Comp3;
        var gridNetEnt = Server.EntMan.GetNetEntity(ProcessEnt);
        var gridCoords = new EntityCoordinates(ProcessEnt, Vector2.Zero);
        var tileIndices = _mapSys.TileIndicesFor(ProcessEnt, gridComp, gridCoords);
        var mixture = SAtmos.GetTileMixture(ProcessEnt, null, tileIndices, true);

        // Get data for client side.
        var cGridEnt = CEntMan.GetEntity(gridNetEnt);
        Assert.That(CTryComp<GasTileOverlayComponent>(cGridEnt, out var cOverlay),
            "Client grid is missing GasTileOverlayComponent");

        // Check if the server actually sent the gas chunks
        await RunUntilSynced();
        Assert.That(cOverlay, Is.Not.Null, "Gas overlay is null on the client.");
        Assert.That(cOverlay.Chunks, Is.Not.Empty, "Gas overlay chunks are empty on the client.");
        return (gridCoords, tileIndices, mixture, cOverlay);
    }
}
