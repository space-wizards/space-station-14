using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.VendingMachines;
using Content.Shared.VendingMachines;

namespace Content.IntegrationTests.Tests.Vending;

public sealed class VendingInteractionTest : InteractionTest
{
    private const string VendingMachineProtoId = "InteractionTestVendingMachine";

    private const string VendedItemProtoId = "InteractionTestItem";

    private const string RestockBoxProtoId = "InteractionTestRestockBox";

    [TestPrototypes]
    private const string TestPrototypes = $@"
- type: entity
  parent: BaseItem
  id: {VendedItemProtoId}
  name: {VendedItemProtoId}

- type: vendingMachineInventory
  id: InteractionTestVendingInventory
  startingInventory:
    {VendedItemProtoId}: 5

- type: entity
  parent: BaseVendingMachineRestock
  id: {RestockBoxProtoId}
  components:
  - type: VendingMachineRestock
    canRestock:
    - InteractionTestVendingInventory

- type: entity
  id: {VendingMachineProtoId}
  parent: VendingMachine
  components:
  - type: VendingMachine
    pack: InteractionTestVendingInventory
    ejectDelay: 0 # no delay to speed up tests
  - type: Sprite
    sprite: error.rsi
";

    [Test]
    public async Task InteractUITest()
    {
        await SpawnTarget(VendingMachineProtoId);

        // Should start with no BUI open
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI was open unexpectedly.");

        // Unpowered vending machine does not open BUI
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI opened without power.");

        // Power the vending machine
        var apc = await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));
        await RunTicks(1);

        // Interacting with powered vending machine opens BUI
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), "BUI failed to open.");

        // Interacting with it again closes the BUI
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI failed to close on interaction.");

        // Reopen BUI for the next check
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), "BUI failed to reopen.");

        // Remove power
        await Delete(apc);
        await RunTicks(1);

        // The BUI should close when power is lost
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI failed to close on power loss.");
    }

    [Test]
    public async Task DispenseItemTest()
    {
        await SpawnTarget(VendingMachineProtoId);
        var vendorEnt = SEntMan.GetEntity(Target.Value);

        var vendingSystem = SEntMan.System<VendingMachineSystem>();
        var items = vendingSystem.GetAllInventory(vendorEnt);

        // Verify initial item count
        Assert.That(items, Is.Not.Empty, $"{VendingMachineProtoId} spawned with no items.");
        Assert.That(items.First().Amount, Is.EqualTo(5), $"{VendingMachineProtoId} spawned with unexpected item count.");

        // Power the vending machine
        await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));
        await RunTicks(1);

        // Open the BUI
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), "BUI failed to open.");

        // Request an item be dispensed
        var ev = new VendingMachineEjectMessage(InventoryType.Regular, VendedItemProtoId);
        await SendBui(VendingMachineUiKey.Key, ev);

        // Make sure the stock decreased
        Assert.That(items.First().Amount, Is.EqualTo(4), "Stocked item count did not decrease.");
        // Make sure the dispensed item was spawned in to the world
        await AssertEntityLookup(
            ("APCBasic", 1),
            (VendedItemProtoId, 1)
        );
    }

    [Test]
    public async Task RestockTest()
    {
        var vendingSystem = SEntMan.System<VendingMachineSystem>();

        await SpawnTarget(VendingMachineProtoId);
        var vendorEnt = SEntMan.GetEntity(Target.Value);

        var items = vendingSystem.GetAllInventory(vendorEnt);

        Assert.That(items, Is.Not.Empty, $"{VendingMachineProtoId} spawned with no items.");
        Assert.That(items.First().Amount, Is.EqualTo(5), $"{VendingMachineProtoId} spawned with unexpected item count.");

        // Try to restock with the maintenance panel closed (nothing happens)
        await InteractUsing(RestockBoxProtoId);

        Assert.That(items.First().Amount, Is.EqualTo(5), "Restocked without opening maintenance panel.");

        // Open the maintenance panel
        await InteractUsing(Screw);

        // Restock the machine
        await InteractUsing(RestockBoxProtoId);

        Assert.That(items.First().Amount, Is.EqualTo(10), "Restocking resulted in unexpected item count.");
    }
}
