using Content.IntegrationTests.Fixtures;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._EE.Plasmaman;

[TestFixture]
public sealed class PlasmamanBreathingTest : GameTest
{
    [Test]
    public async Task PlasmamanBreathesPlasmaFromTank()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var stationSystem = entManager.System<StationSpawningSystem>();
        var inventorySystem = entManager.System<InventorySystem>();
        var internals = entManager.System<InternalsSystem>();
        var testMap = await Pair.CreateTestMap();

        EntityUid plasmaman = default;
        float initialMoles = 0f;

        await server.WaitAssertion(() =>
        {
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Plasmaman", Sex.Unsexed);
            plasmaman = stationSystem.SpawnPlayerMob(testMap.GridCoords, "Passenger", profile, station: null);

            Assert.That(internals.AreInternalsWorking(plasmaman), Is.True, "Internals not working at spawn.");
            Assert.That(inventorySystem.TryGetSlotEntity(plasmaman, "pocket1", out var tank), Is.True, "No tank in pocket1.");
            Assert.That(entManager.TryGetComponent<GasTankComponent>(tank!.Value, out var gasTank), Is.True, "Tank missing GasTankComponent.");
            initialMoles = gasTank!.Air.TotalMoles;
            Assert.That(initialMoles, Is.GreaterThan(0f), "Tank starts empty.");
        });

        await Pair.RunSeconds(5f);

        await server.WaitAssertion(() =>
        {
            Assert.That(entManager.TryGetComponent<RespiratorComponent>(plasmaman, out var respirator), Is.True);
            Assert.That(respirator!.Saturation, Is.GreaterThan(0f), $"Saturation should be positive, got {respirator.Saturation}.");

            Assert.That(inventorySystem.TryGetSlotEntity(plasmaman, "pocket1", out var tank), Is.True);
            var gasTank = entManager.GetComponent<GasTankComponent>(tank!.Value);
            var consumed = initialMoles - gasTank.Air.TotalMoles;
            var perSecond = consumed / 5f;
            var minutesToEmpty = initialMoles / perSecond / 60f;
            Assert.That(gasTank.Air.TotalMoles, Is.LessThan(initialMoles), $"Tank moles did not decrease: {gasTank.Air.TotalMoles} vs initial {initialMoles}.");
            // expected ~19.5 min full duration at idle.
            Assert.That(minutesToEmpty, Is.GreaterThan(10f), $"Tank should last >10 min idle, got {minutesToEmpty:F1} min. perSec={perSecond:F4}");
        });
    }
}
