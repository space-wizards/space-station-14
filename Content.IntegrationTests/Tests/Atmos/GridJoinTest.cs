using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
public sealed class GridJoinTest
{
    private const string CanisterProtoId = "AirCanister";

    [Test]
    public async Task TestGridJoinAtmosphere()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var atmosSystem = entMan.System<AtmosphereSystem>();
        var atmosDeviceSystem = entMan.System<AtmosDeviceSystem>();
        var transformSystem = entMan.System<SharedTransformSystem>();

        var testMap = await pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            // Spawn an atmos device on the grid
            var canister = entMan.Spawn(CanisterProtoId);
            transformSystem.SetCoordinates(canister, testMap.GridCoords);
            var deviceComp = entMan.GetComponent<AtmosDeviceComponent>(canister);
            var canisterEnt = (canister, deviceComp);

            // Make sure the canister is tracked as an off-grid device
            Assert.That(atmosDeviceSystem.IsJoinedOffGrid(canisterEnt));

            // Add an atmosphere to the grid
            entMan.AddComponent<GridAtmosphereComponent>(testMap.Grid);

            // Force AtmosDeviceSystem to update off-grid devices
            // This means the canister is now considered on-grid,
            // but it's still tracked as off-grid!
            Assert.DoesNotThrow(() => atmosDeviceSystem.Update(atmosSystem.AtmosTime));

            // Make sure that the canister is now properly tracked as on-grid
            Assert.That(atmosDeviceSystem.IsJoinedOffGrid(canisterEnt), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
