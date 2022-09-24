using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Content.Shared.VendingMachines;
using Content.Server.VendingMachineRestockPackage;
using Content.Server.VendingMachines;
using Content.Server.Wires;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(VendingMachineRestockPackageComponent))]
    public sealed class VendingMachineRestockTest : EntitySystem
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Hands
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso

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
  - type: VendingMachineRestockPackage
    canRestock:
    - OtherTestInventory

- type: entity
  parent: BaseItem
  id: TestRestockCorrect
  name: TestRestockCorrect
  components:
  - type: VendingMachineRestockPackage
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

            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<VendingMachineRestockPackageComponent>(out var package))
                        continue;

                    foreach(var vendingInventory in package.CanRestock)
                    {
                        Assert.That(protoManager.HasIndex<VendingMachineInventoryPrototype>(vendingInventory), $"Unknown VendingMachineInventoryPrototype {vendingInventory} on restock package {proto.ID}");
                    }
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestCompleteRestockProcess()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            EntityUid packageRight;
            EntityUid packageWrong;
            EntityUid machine;
            EntityUid user;
            VendingMachineComponent machineComponent;
            VendingMachineRestockPackageComponent restockRightComponent;
            VendingMachineRestockPackageComponent restockWrongComponent;
            WiresComponent machineWires;

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                user = entityManager.SpawnEntity("HumanDummy", coordinates);
                machine = entityManager.SpawnEntity("VendingMachineTest", coordinates);
                packageRight = entityManager.SpawnEntity("TestRestockCorrect", coordinates);
                packageWrong = entityManager.SpawnEntity("TestRestockWrong", coordinates);

                // Test for components existing
                Assert.True(entityManager.TryGetComponent(machine, out machineComponent!), $"Machine has no {nameof(VendingMachineComponent)}");
                Assert.True(entityManager.TryGetComponent(packageRight, out restockRightComponent!), $"Correct package has no {nameof(VendingMachineRestockPackageComponent)}");
                Assert.True(entityManager.TryGetComponent(packageWrong, out restockWrongComponent!), $"Wrong package has no {nameof(VendingMachineRestockPackageComponent)}");
                Assert.True(entityManager.TryGetComponent(machine, out machineWires), $"Machine has no {nameof(WiresComponent)}");

                var systemRestock = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<VendingMachineRestockPackageSystem>();
                var systemMachine = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<VendingMachineSystem>();

                // test that panel needs to be opened first
                Assert.That(systemRestock.TryAccessMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.False, "Right package is able to restock without opened access panel");
                Assert.That(systemRestock.TryAccessMachine(packageWrong, restockWrongComponent, machineComponent, user, machine), Is.False, "Wrong package is able to restock without opened access panel");

                // open panel the panel and
                machineWires.IsPanelOpen = true;

                // test that the right package works for the right machine
                Assert.That(systemRestock.TryAccessMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.True, "Correct package is unable to restock with access panel opened");

                // test that wrong package does not work
                Assert.That(systemRestock.TryMatchPackageToMachine(packageWrong, restockWrongComponent, machineComponent, user, machine), Is.False, "Package with invalid canRestock is able to restock machine");

                // test that right package does work
                Assert.That(systemRestock.TryMatchPackageToMachine(packageRight, restockRightComponent, machineComponent, user, machine), Is.True, "Package with valid canRestock is unable to restock machine");

                // make sure there's something in there to begin with
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent).Count, Is.GreaterThan(0), "Machine inventory is empty before emptying");

                // empty the inventory and
                systemMachine.EjectRandom(machine, false, true, machineComponent);
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent).Count, Is.EqualTo(0), "Machine inventory is not empty after ejecting");

                // test that inventory is actually restocked
                systemMachine.TryRestockInventory(machine, machineComponent);
                Assert.That(systemMachine.GetAvailableInventory(machine, machineComponent).Count, Is.GreaterThan(0), "Machine available inventory count is not greater than zero after restock");
            });

        }
    }
}

