using Content.Server.Light.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// GasTileOverlay is being tested here for networking of atmos fires
/// </summary>
public sealed class SharedGasTileFireOverlaySystemTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    [Test]
    public async Task TestGasTileFireOverlayDataSync()
    {
        var sMapSys = Server.System<SharedMapSystem>();

        var gridNetEnt = Server.EntMan.GetNetEntity(ProcessEnt);

        var gridCoords = new EntityCoordinates(ProcessEnt, Vector2.Zero);
        var tileIndices = sMapSys.TileIndicesFor(ProcessEnt, ProcessEnt.Comp3, gridCoords);
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
            mixture.AdjustMoles(Gas.Plasma, 100f);
            mixture.AdjustMoles(Gas.Oxygen, 100f); // Inject flamable gasses

            var welder = SEntMan.SpawnEntity("Welder", gridCoords);
            Assert.That(ItemToggleSys.TryActivate(welder)); //ignite em
        });

        await RunTicks(60);
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

            Assert.That(tile.FireState, Is.GreaterThan(0), $"Tile at {tileIndices} is not set on fire!");
        });
    }
}
