using System.Collections.Generic;
using Content.Server.Station.Systems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Preferences;

[TestFixture]
public sealed class LoadoutTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: playTimeTracker
  id: PlayTimeLoadoutTester

- type: loadout
  id: TestJumpsuit
  equipment:
    jumpsuit: ClothingUniformJumpsuitColorGrey

- type: loadoutGroup
  id: LoadoutTesterJumpsuit
  name: generic-unknown
  loadouts:
  - TestJumpsuit

- type: roleLoadout
  id: JobLoadoutTester
  groups:
  - LoadoutTesterJumpsuit

- type: job
  id: LoadoutTester
  playTimeTracker: PlayTimeLoadoutTester
";

    private readonly Dictionary<string, EntProtoId> _expectedEquipment = new()
    {
        ["jumpsuit"] = "ClothingUniformJumpsuitColorGrey"
    };

    /// <summary>
    /// Checks that an empty loadout still spawns with default gear and not naked.
    /// </summary>
    [Test]
    public async Task TestEmptyLoadout()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings()
        {
            Dirty = true,
        });
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();

        // Check that an empty role loadout spawns gear
        var stationSystem = entManager.System<StationSpawningSystem>();
        var inventorySystem = entManager.System<InventorySystem>();
        var testMap = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var profile = new HumanoidCharacterProfile();

            profile.SetLoadout(new RoleLoadout("LoadoutTester"));

            var tester = stationSystem.SpawnPlayerMob(testMap.GridCoords, job: "LoadoutTester", profile, station: null);

            var slotQuery = inventorySystem.GetSlotEnumerator(tester);
            var checkedCount = 0;
            while (slotQuery.NextItem(out var item, out var slot))
            {
                // Make sure the slot is valid
                Assert.That(_expectedEquipment.TryGetValue(slot.Name, out var expectedItem), $"Spawned item in unexpected slot: {slot.Name}");

                // Make sure that the item is the right one
                var meta = entManager.GetComponent<MetaDataComponent>(item);
                Assert.That(meta.EntityPrototype.ID, Is.EqualTo(expectedItem.Id), $"Spawned wrong item in slot {slot.Name}!");

                checkedCount++;
            }
            // Make sure the number of items is the same
            Assert.That(checkedCount, Is.EqualTo(_expectedEquipment.Count), "Number of items does not match expected!");

            entManager.DeleteEntity(tester);
        });

        await pair.CleanReturnAsync();
    }
}
