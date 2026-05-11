using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Body.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._EE.Plasmaman;

[TestFixture]
public sealed class PlasmamanSpawnGearTest : GameTest
{
    [Test]
    public async Task ExistingHumanSurvivalLoadoutBecomesPlasmamanEnviroGear()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var stationSystem = entManager.System<StationSpawningSystem>();
        var inventorySystem = entManager.System<InventorySystem>();
        var internals = entManager.System<InternalsSystem>();
        var testMap = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Plasmaman", Sex.Unsexed);
            var oldLoadout = new RoleLoadout("JobPassenger");
            oldLoadout.SelectedLoadouts["PassengerJumpsuit"] = new List<Loadout>
            {
                new() { Prototype = "GreyJumpsuit" },
            };
            oldLoadout.SelectedLoadouts["Survival"] = new List<Loadout>
            {
                new() { Prototype = "EmergencyOxygen" },
            };
            profile.SetLoadout(oldLoadout);

            var plasmaman = stationSystem.SpawnPlayerMob(testMap.GridCoords, "Passenger", profile, station: null);

            AssertSlotPrototype(inventorySystem, entManager, plasmaman, "jumpsuit", "ClothingUniformEnvirosuit");
            AssertSlotPrototype(inventorySystem, entManager, plasmaman, "head", "ClothingHeadEnvirohelm");
            AssertSlotPrototype(inventorySystem, entManager, plasmaman, "gloves", "ClothingHandsGlovesEnvirogloves");
            AssertSlotPrototype(inventorySystem, entManager, plasmaman, "mask", "ClothingMaskBreath");
            AssertSlotPrototype(inventorySystem, entManager, plasmaman, "pocket1", "DoubleEmergencyPlasmaTankFilled");
            AssertSlotPrototype(inventorySystem, entManager, plasmaman, "id", "PassengerPDA");
            Assert.That(internals.AreInternalsWorking(plasmaman), Is.True);
        });
    }

    private static void AssertSlotPrototype(
        InventorySystem inventorySystem,
        IEntityManager entManager,
        EntityUid uid,
        string slot,
        string prototype)
    {
        Assert.That(inventorySystem.TryGetSlotEntity(uid, slot, out var item), Is.True, $"Missing item in {slot}.");

        var meta = entManager.GetComponent<MetaDataComponent>(item!.Value);
        Assert.That(meta.EntityPrototype?.ID, Is.EqualTo(prototype), $"Wrong item in {slot}.");
    }
}
