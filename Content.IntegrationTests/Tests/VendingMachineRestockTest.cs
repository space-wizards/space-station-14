#nullable enable
using System.Collections.Generic;
using Content.Server.VendingMachines;
using Content.Server.Wires;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Prototypes;
using Content.Shared.Storage.Components;
using Content.Shared.VendingMachines;
using Content.Shared.Wires;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(VendingMachineRestockComponent))]
    [TestOf(typeof(VendingMachineSystem))]
    public sealed class VendingMachineRestockTest : EntitySystem
    {
        private static readonly ProtoId<DamageTypePrototype> TestDamageType = "Blunt";

        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanVendingDummy
  id: HumanVendingDummy
  components:
  - type: Hands
  - type: ComplexInteraction
  - type: Body
    prototype: Human

- type: entity
  parent: FoodSnackBase
  id: TestRamen
  name: TestRamen

- type: vendingMachineInventory
  id: TestInventory
  startingInventory:
    TestRamen: 1

- type: vendingMachineInventory
  id: OtherTestInventory
  startingInventory:
    TestRamen: 3

- type: vendingMachineInventory
  id: BigTestInventory
  startingInventory:
    TestRamen: 4

- type: entity
  parent: BaseVendingMachineRestock
  id: TestRestockWrong
  name: TestRestockWrong
  components:
  - type: VendingMachineRestock
    canRestock:
    - OtherTestInventory

- type: entity
  parent: BaseVendingMachineRestock
  id: TestRestockCorrect
  name: TestRestockCorrect
  components:
  - type: VendingMachineRestock
    canRestock:
    - TestInventory

- type: entity
  parent: BaseVendingMachineRestock
  id: TestRestockExplode
  name: TestRestockExplode
  components:
  - type: Damageable
    damageContainer: Inorganic
    damageModifierSet: Metallic
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 20
      behaviors:
      - !type:DumpRestockInventory
      - !type:DoActsBehavior
        acts: [ 'Destruction' ]
  - type: VendingMachineRestock
    canRestock:
    - BigTestInventory

- type: entity
  parent: VendingMachine
  id: VendingMachineTest
  name: Test Ramen
  components:
  - type: Wires
    layoutId: Vending
  - type: VendingMachine
    pack: TestInventory
  - type: Sprite
    sprite: error.rsi
";

        [Test]
        public async Task TestAllRestocksAreAvailableToBuy()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var compFact = server.ResolveDependency<IComponentFactory>();

            await server.WaitAssertion(() =>
            {
                HashSet<string> restocks = new();
                Dictionary<string, List<string>> restockStores = new();

                // Collect all the prototypes with restock components.
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract
                        || pair.IsTestPrototype(proto)
                        || !proto.HasComponent<VendingMachineRestockComponent>())
                    {
                        continue;
                    }

                    restocks.Add(proto.ID);
                }

                // Collect all the prototypes with StorageFills referencing those entities.
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<StorageFillComponent>(out var storage, compFact))
                        continue;

                    List<string> restockStore = new();
                    foreach (var spawnEntry in storage.Contents)
                    {
                        if (spawnEntry.PrototypeId != null && restocks.Contains(spawnEntry.PrototypeId))
                            restockStore.Add(spawnEntry.PrototypeId);
                    }

                    if (restockStore.Count > 0)
                        restockStores.Add(proto.ID, restockStore);
                }

                // Iterate through every CargoProduct and make sure each
                // prototype with a restock component is referenced in a
                // purchaseable entity with a StorageFill.
                foreach (var proto in prototypeManager.EnumeratePrototypes<CargoProductPrototype>())
                {
                    if (restockStores.ContainsKey(proto.Product))
                    {
                        foreach (var entry in restockStores[proto.Product])
                            restocks.Remove(entry);

                        restockStores.Remove(proto.Product);
                    }
                }

                Assert.Multiple(() =>
                {
                    Assert.That(restockStores, Has.Count.EqualTo(0),
                        $"Some entities containing entities with VendingMachineRestock components are unavailable for purchase: \n - {string.Join("\n - ", restockStores.Keys)}");

                    Assert.That(restocks, Has.Count.EqualTo(0),
                        $"Some entities with VendingMachineRestock components are unavailable for purchase: \n - {string.Join("\n - ", restocks)}");
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestCompleteRestockProcess()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var mapSystem = server.System<SharedMapSystem>();

            EntityUid packageRight;
            EntityUid packageWrong;
            EntityUid machine;
            EntityUid user;
            VendingMachineComponent machineComponent = default!;
            VendingMachineRestockComponent restockRightComponent = default!;
            VendingMachineRestockComponent restockWrongComponent = default!;
            WiresPanelComponent machineWiresPanel = default!;

            var testMap = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                // Spawn the entities.
                user = entityManager.SpawnEntity("HumanVendingDummy", coordinates);
                machine = entityManager.SpawnEntity("VendingMachineTest", coordinates);
                packageRight = entityManager.SpawnEntity("TestRestockCorrect", coordinates);
                packageWrong = entityManager.SpawnEntity("TestRestockWrong", coordinates);

                // Sanity test for components existing.
                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(machine, out machineComponent!), $"Machine has no {nameof(VendingMachineComponent)}");
                    Assert.That(entityManager.TryGetComponent(packageRight, out restockRightComponent!), $"Correct package has no {nameof(VendingMachineRestockComponent)}");
                    Assert.That(entityManager.TryGetComponent(packageWrong, out restockWrongComponent!), $"Wrong package has no {nameof(VendingMachineRestockComponent)}");
                    Assert.That(entityManager.TryGetComponent(machine, out machineWiresPanel!), $"Machine has no {nameof(WiresPanelComponent)}");
                });

                var systemMachine = entitySystemManager.GetEntitySystem<VendingMachineSystem>();

                // Test that the panel needs to be opened first.
                Assert.Multiple(() =>
                {
                    Assert.That(systemMachine.TryAccessMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.False, "Right package is able to restock without opened access panel");
                    Assert.That(systemMachine.TryAccessMachine(packageWrong, restockWrongComponent, machineComponent, user, machine), Is.False, "Wrong package is able to restock without opened access panel");
                });

                var systemWires = entitySystemManager.GetEntitySystem<WiresSystem>();
                // Open the panel.
                systemWires.TogglePanel(machine, machineWiresPanel, true);

                Assert.Multiple(() =>
                {
                    // Test that the right package works for the right machine.
                    Assert.That(systemMachine.TryAccessMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.True, "Correct package is unable to restock with access panel opened");

                    // Test that the wrong package does not work.
                    Assert.That(systemMachine.TryMatchPackageToMachine(packageWrong, restockWrongComponent, machineComponent, user, machine), Is.False, "Package with invalid canRestock is able to restock machine");

                    // Test that the right package does work.
                    Assert.That(systemMachine.TryMatchPackageToMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.True, "Package with valid canRestock is unable to restock machine");

                    // Make sure there's something in there to begin with.
                    Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent), Has.Count.GreaterThan(0),
                        "Machine inventory is empty before emptying.");
                });

                // Empty the inventory.
                systemMachine.EjectRandom(machine, false, true, machineComponent);
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent), Has.Count.EqualTo(0),
                    "Machine inventory is not empty after ejecting.");

                // Test that the inventory is actually restocked.
                systemMachine.TryRestockInventory(machine, machineComponent);
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent), Has.Count.GreaterThan(0),
                    "Machine available inventory count is not greater than zero after restock.");

                mapSystem.DeleteMap(testMap.MapId);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestRestockBreaksOpen()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var mapSystem = server.System<SharedMapSystem>();

            var damageableSystem = entitySystemManager.GetEntitySystem<DamageableSystem>();

            var testMap = await pair.CreateTestMap();

            EntityUid restock = default;

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                var totalStartingRamen = 0;

                foreach (var meta in entityManager.EntityQuery<MetaDataComponent>())
                    if (!meta.Deleted && meta.EntityPrototype?.ID == "TestRamen")
                        totalStartingRamen++;

                Assert.That(totalStartingRamen, Is.EqualTo(0),
                    "Did not start with zero ramen.");

                restock = entityManager.SpawnEntity("TestRestockExplode", coordinates);
                var damageSpec = new DamageSpecifier(prototypeManager.Index(TestDamageType), 100);
                var damageResult = damageableSystem.ChangeDamage(restock, damageSpec);

#pragma warning disable NUnit2045
                Assert.That(!damageResult.Empty, "Received empty damageResult when attempting to damage restock box.");

                Assert.That((int) damageResult.GetTotal(), Is.GreaterThan(0), "Box damage result was not greater than 0.");
#pragma warning restore NUnit2045
            });
            await server.WaitRunTicks(15);
            await server.WaitAssertion(() =>
            {
                Assert.That(entityManager.Deleted(restock),
                    "Restock box was not deleted after being damaged.");

                var totalRamen = 0;

                foreach (var meta in entityManager.EntityQuery<MetaDataComponent>())
                    if (!meta.Deleted && meta.EntityPrototype?.ID == "TestRamen")
                        totalRamen++;

                Assert.That(totalRamen, Is.EqualTo(2),
                    "Did not find enough ramen after destroying restock box.");

                mapSystem.DeleteMap(testMap.MapId);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestRestockInventoryBounds()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var vendingMachineSystem = entitySystemManager.GetEntitySystem<SharedVendingMachineSystem>();

            var testMap = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                var machine = entityManager.SpawnEntity("VendingMachineTest", coordinates);

                Assert.That(vendingMachineSystem.GetAvailableInventory(machine), Has.Count.EqualTo(1),
                    "Machine's available inventory did not contain one entry.");

                Assert.That(vendingMachineSystem.GetAvailableInventory(machine)[0].Amount, Is.EqualTo(1),
                    "Machine's available inventory is not the expected amount.");

                vendingMachineSystem.RestockInventoryFromPrototype(machine);

                Assert.That(vendingMachineSystem.GetAvailableInventory(machine)[0].Amount, Is.EqualTo(2),
                    "Machine's available inventory is not double its starting amount after a restock.");

                vendingMachineSystem.RestockInventoryFromPrototype(machine);

                Assert.That(vendingMachineSystem.GetAvailableInventory(machine)[0].Amount, Is.EqualTo(3),
                    "Machine's available inventory is not triple its starting amount after two restocks.");

                vendingMachineSystem.RestockInventoryFromPrototype(machine);

                Assert.That(vendingMachineSystem.GetAvailableInventory(machine)[0].Amount, Is.EqualTo(3),
                    "Machine's available inventory did not stay the same after a third restock.");
            });

            await pair.CleanReturnAsync();
        }
    }
}

#nullable disable
