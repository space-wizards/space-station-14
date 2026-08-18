#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.VendingMachines;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.VendingMachines;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Vending;

public sealed class VendingInteractionTest : InteractionTest
{
    // Entity prototypes
    private const string VendingMachineProtoId = "InteractionTestVendingMachine";
    private const string VendedItemProtoId = "InteractionTestItem";
    private const string RestockBoxProtoId = "InteractionTestRestockBox";
    private const string RestockBoxOtherProtoId = "InteractionTestRestockBoxOther";
    private const string APCProtoId = "APCBasic";
    // Vending machine inventory prototypes
    private const string Pack1 = "InteractionTestVendingInventory";
    private const string Pack2 = "InteractionTestVendingInventoryOther";
    private static readonly ProtoId<DamageTypePrototype> TestDamageType = "Blunt";

    [TestPrototypes]
    private const string TestPrototypes = $@"
- type: entity
  parent: BaseItem
  id: {VendedItemProtoId}
  name: {VendedItemProtoId}

- type: vendingMachineInventory
  id: {Pack1}
  startingInventory:
    {VendedItemProtoId}: 5

- type: vendingMachineInventory
  id: {Pack2}
  startingInventory:
    {VendedItemProtoId}: 5

- type: entity
  parent: BaseVendingMachineRestock
  id: {RestockBoxProtoId}
  components:
  - type: VendingMachineRestock
    canRestock:
    - {Pack1}

- type: entity
  parent: BaseVendingMachineRestock
  id: {RestockBoxOtherProtoId}
  components:
  - type: VendingMachineRestock
    canRestock:
    - {Pack2}

- type: entity
  parent: BaseVendingMachine
  id: {VendingMachineProtoId}
  components:
  - type: VendingMachine
    pack: {Pack1}
  - type: VendingMachineEject
    ejectDelay: 0 # no delay to speed up tests
  - type: Sprite
    sprite: error.rsi
";

    [SidedDependency(Side.Server)] private DamageableSystem _sDamageable = default!;
    [SidedDependency(Side.Server)] private VendingMachineSystem _sVending = default!;

    [SidedDependency(Side.Server)] private EntityQuery<DamageableComponent> _sQuery = default!;

    [Test]
    [Description("Tests that vending machines' UI opens when used in the world.")]
    public async Task InteractUITest()
    {
        await SpawnTarget(VendingMachineProtoId);

        // Should start with no BUI open
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI was open unexpectedly.");

        // Unpowered vending machine does not open BUI
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI opened without power.");

        // Power the vending machine
        var apc = await SpawnEntity(APCProtoId, SEntMan.GetCoordinates(TargetCoords));
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
    [Description("Tests that vending machines can dispense items and account for them properly.")]
    public async Task DispenseItemTest()
    {
        await SpawnTarget(VendingMachineProtoId);
        var vendorEnt = SEntMan.GetEntity(Target.Value);

        var items = _sVending.GetAllInventory(vendorEnt);

        // Verify initial item count
        Assert.That(items, Is.Not.Empty, $"{VendingMachineProtoId} spawned with no items.");
        Assert.That(items.First().Amount, Is.EqualTo(5), $"{VendingMachineProtoId} spawned with unexpected item count.");

        // Power the vending machine
        await SpawnEntity(APCProtoId, SEntMan.GetCoordinates(TargetCoords));
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
            (APCProtoId, 1),
            (VendedItemProtoId, 1)
        );
    }

    [Test]
    [Description("Tests that vending machines can be restocked.")]
    public async Task RestockTest()
    {
        await SpawnTarget(VendingMachineProtoId);
        var vendorEnt = ToServer(Target.Value);

        var items = _sVending.GetAllInventory(vendorEnt);

        Assert.That(items, Is.Not.Empty, $"{VendingMachineProtoId} spawned with no items.");
        Assert.That(items.First().Amount, Is.EqualTo(5), $"{VendingMachineProtoId} spawned with unexpected item count.");

        // Try to restock with the maintenance panel closed (nothing happens)
        await InteractUsing(RestockBoxProtoId);

        Assert.That(items.First().Amount, Is.EqualTo(5), "Restocked without opening maintenance panel.");

        // Open the maintenance panel
        await InteractUsing(Screw);

        // Try to restock using the wrong restock box (nothing happens)
        await InteractUsing(RestockBoxOtherProtoId);

        Assert.That(items.First().Amount, Is.EqualTo(5), "Restocked with wrong restock box.");

        // Restock the machine
        await InteractUsing(RestockBoxProtoId);

        Assert.That(items.First().Amount, Is.EqualTo(10), "Restocking resulted in unexpected item count.");
    }

    [Test]
    [Description("Tests that vending machines' interfaces work after being repaired.")]
    public async Task RepairTest()
    {
        await SpawnTarget(VendingMachineProtoId);

        // Power the vending machine
        await SpawnEntity(APCProtoId, SEntMan.GetCoordinates(TargetCoords));
        await RunTicks(1);

        // Break it
        await BreakVendor();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "BUI did not close when vending machine broke.");

        // Make sure we can't open the BUI while it's broken
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), Is.False, "Opened BUI of broken vending machine.");

        // Repair the vending machine
        await InteractUsing(Weld);

        // Make sure the BUI can open now that the machine has been repaired
        await Activate();
        Assert.That(IsUiOpen(VendingMachineUiKey.Key), "Failed to open BUI after repair.");
    }

    private async Task BreakVendor()
    {
        Assert.That(_sQuery.TryComp(STarget, out var damageable), Is.True, $"{VendingMachineProtoId} does not have DamageableComponent.");
        Entity<DamageableComponent> sDamageableTarget = (STarget!.Value, damageable!);
        Assert.That(_sDamageable.GetPositiveDamage(sDamageableTarget).GetTotal(), Is.EqualTo(FixedPoint2.Zero), $"{VendingMachineProtoId} started with unexpected damage.");

        // Damage the vending machine to the point that it breaks
        var damageType = ProtoMan.Index(TestDamageType);
        var damage = new DamageSpecifier(damageType, FixedPoint2.New(100));
        await Server.WaitPost(() => _sDamageable.TryChangeDamage(sDamageableTarget.AsNullable(), damage, ignoreResistances: true));
        await RunTicks(5);
        Assert.That(_sDamageable.GetPositiveDamage(sDamageableTarget).GetTotal(), Is.GreaterThan(FixedPoint2.Zero), $"{VendingMachineProtoId} did not take damage.");
    }
}
