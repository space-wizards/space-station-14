using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Server.Storage.Components;
using Content.Server.VendingMachines;
using Content.Server.VendingMachines.Restock;
using Content.Server.Wires;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.VendingMachines;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(VendingMachineRestockComponent))]
    [TestOf(typeof(VendingMachineRestockSystem))]
    public sealed class VendingMachineRestockTest : EntitySystem
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Hands
  - type: Body
    prototype: Human

- type: entity
  parent: BaseItem
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

- type: entity
  parent: BaseItem
  id: TestRestockWrong
  name: TestRestockWrong
  components:
  - type: VendingMachineRestock
    canRestock:
    - OtherTestInventory

- type: entity
  parent: BaseItem
  id: TestRestockCorrect
  name: TestRestockCorrect
  components:
  - type: VendingMachineRestock
    canRestock:
    - TestInventory

- type: entity
  parent: VendingMachine
  id: VendingMachineTest
  name: Test Ramen
  components:
  - type: VendingMachine
    pack: TestInventory
  - type: Sprite
    sprite: error.rsi
";

        [Test]
        public async Task TestCanRestockIsValid()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            // Per RobustIntegrationTest.cs, wait until state is settled to access it.
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<VendingMachineRestockComponent>(out var package))
                        continue;

                    foreach(var vendingInventory in package.CanRestock)
                    {
                        Assert.That(prototypeManager.HasIndex<VendingMachineInventoryPrototype>(vendingInventory),
                            $"Unknown VendingMachineInventoryPrototype {vendingInventory} on VendingMachineRestockComponent for {proto.ID}");
                    }
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestAllRestocksAreAvailableToBuy()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                HashSet<string> restocks = new();
                Dictionary<string, List<string>> restockStores = new();

                // Collect all the prototypes with restock components.
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<VendingMachineRestockComponent>(out var restock)
                        || proto.Abstract)
                        continue;

                    restocks.Add(proto.ID);
                }

                // Collect all the prototypes with StorageFills referencing those entities.
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<StorageFillComponent>(out var storage))
                        continue;

                    List<string> restockStore = new();
                    foreach (var spawnEntry in storage.Contents)
                    {
                        if (restocks.Contains(spawnEntry.PrototypeId))
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

                Assert.That(restockStores.Count, Is.EqualTo(0),
                    $"Some entities containing entities with VendingMachineRestock components are unavailable for purchase: \n - {string.Join("\n - ", restockStores.Keys)}");

                Assert.That(restocks.Count, Is.EqualTo(0),
                    $"Some entities with VendingMachineRestock components are unavailable for purchase: \n - {string.Join("\n - ", restocks)}");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestCompleteRestockProcess()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            EntityUid packageRight;
            EntityUid packageWrong;
            EntityUid machine;
            EntityUid user;
            VendingMachineComponent machineComponent;
            VendingMachineRestockComponent restockRightComponent;
            VendingMachineRestockComponent restockWrongComponent;
            WiresComponent machineWires;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                // Spawn the entities.
                user = entityManager.SpawnEntity("HumanDummy", coordinates);
                machine = entityManager.SpawnEntity("VendingMachineTest", coordinates);
                packageRight = entityManager.SpawnEntity("TestRestockCorrect", coordinates);
                packageWrong = entityManager.SpawnEntity("TestRestockWrong", coordinates);

                // Sanity test for components existing.
                Assert.True(entityManager.TryGetComponent(machine, out machineComponent!), $"Machine has no {nameof(VendingMachineComponent)}");
                Assert.True(entityManager.TryGetComponent(packageRight, out restockRightComponent!), $"Correct package has no {nameof(VendingMachineRestockComponent)}");
                Assert.True(entityManager.TryGetComponent(packageWrong, out restockWrongComponent!), $"Wrong package has no {nameof(VendingMachineRestockComponent)}");
                Assert.True(entityManager.TryGetComponent(machine, out machineWires), $"Machine has no {nameof(WiresComponent)}");

                var systemRestock = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<VendingMachineRestockSystem>();
                var systemMachine = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<VendingMachineSystem>();

                // Test that the panel needs to be opened first.
                Assert.That(systemRestock.TryAccessMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.False, "Right package is able to restock without opened access panel");
                Assert.That(systemRestock.TryAccessMachine(packageWrong, restockWrongComponent, machineComponent, user, machine), Is.False, "Wrong package is able to restock without opened access panel");

                // Open the panel.
                machineWires.IsPanelOpen = true;

                // Test that the right package works for the right machine.
                Assert.That(systemRestock.TryAccessMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.True, "Correct package is unable to restock with access panel opened");

                // Test that the wrong package does not work.
                Assert.That(systemRestock.TryMatchPackageToMachine(packageWrong, restockWrongComponent, machineComponent, user, machine), Is.False, "Package with invalid canRestock is able to restock machine");

                // Test that the right package does work.
                Assert.That(systemRestock.TryMatchPackageToMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.True, "Package with valid canRestock is unable to restock machine");

                // Make sure there's something in there to begin with.
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent).Count, Is.GreaterThan(0),
                    "Machine inventory is empty before emptying.");

                // Empty the inventory.
                systemMachine.EjectRandom(machine, false, true, machineComponent);
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent).Count, Is.EqualTo(0),
                    "Machine inventory is not empty after ejecting.");

                // Test that the inventory is actually restocked.
                systemMachine.TryRestockInventory(machine, machineComponent);
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent).Count, Is.GreaterThan(0),
                    "Machine available inventory count is not greater than zero after restock.");
            });

        }
    }
}

