using System.Diagnostics;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
public sealed class GridJoinTest : AtmosTest
{
    private readonly EntProtoId _canisterProtoId = "AirCanister";

    [SidedDependency(Side.Server)] private readonly AtmosDeviceSystem _atmosDeviceSystem = default!;
    [SidedDependency(Side.Server)] private readonly SharedTransformSystem _transformSystem = default!;

    [Test]
    public async Task TestGridJoinAtmosphere()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(delegate
        {
            // Spawn an atmos device on the grid
            var canister = SSpawn(_canisterProtoId);
            Debug.Assert(TestMap != null, nameof(TestMap) + " != null");
            _transformSystem.SetCoordinates(canister, TestMap.GridCoords);
            var deviceComp = SEntMan.GetComponent<AtmosDeviceComponent>(canister);
            var canisterEnt = (canister, deviceComp);

            // Make sure the canister is tracked as an off-grid device
            Assert.That(_atmosDeviceSystem.IsJoinedOffGrid(canisterEnt));

            // Add an atmosphere to the grid
            SEntMan.AddComponent<GridAtmosphereComponent>(TestMap.Grid);

            // Force AtmosDeviceSystem to update off-grid devices
            // This means the canister is now considered on-grid,
            // but it's still tracked as off-grid!
            Assert.DoesNotThrow(() => _atmosDeviceSystem.Update(SAtmos.AtmosTime));

            // Make sure that the canister is now properly tracked as on-grid
            Assert.That(_atmosDeviceSystem.IsJoinedOffGrid(canisterEnt), Is.False);
        });
    }
}
