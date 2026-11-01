#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chameleon;

public sealed class ChameleonClothingTest : GameTest
{
    private static readonly string[] ChameleonClothingEntities = GameDataScrounger.EntitiesWithComponent("ChameleonClothing");

    [SidedDependency(Side.Server)] private readonly SharedChameleonClothingSystem _chameleonClothingSys = null!;
    [SidedDependency(Side.Server)] private readonly InventorySystem _inventorySys = null!;

    // A dummy clothing entity that we can use to fill slots that other slots depend on (i.e. jumpsuit)
    private const string SlotFillerId = "ClothingDummySlotFiller";
    private const string TestDummyId = "TestDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TestDummyId}
  name: {TestDummyId}
  parent: BaseSpeciesAppearance

- type: entity
  id: {SlotFillerId}
  name: {SlotFillerId}
  parent: Clothing
  components:
  - type: Clothing
    slots: [all]
";

    [TestCaseSource(nameof(ChameleonClothingEntities))]
    [TestOf(typeof(SharedChameleonClothingSystem))]
    [Description("Activates all available appearance options for a chameleon clothing item.")]
    [TrackingIssue("https://github.com/space-wizards/space-station-14/issues/43365")]
    [Ignore("Client has missing RSI state errors when switching between certain options.")]
    public async Task ActivateAllOptions(string protoId)
    {
        // Spawn a test dummy who will equip the item.
        var player = await Spawn(TestDummyId);
        // Spawn the item to test.
        var ent = await Spawn(protoId);

        var comp = SComp<ChameleonClothingComponent>(ent);
        var invComp = SComp<InventoryComponent>(player);

        await Server.WaitAssertion(() =>
        {
            // Try to equip the item into each inventory slot until we find one that works.
            var equipped = false;
            foreach (var slot in invComp.Slots)
            {
                // If the slot depends on another slot being filled, stuff a dummy entity into it.
                if (!string.IsNullOrEmpty(slot.DependsOn) && !_inventorySys.TryGetSlotEntity(player, slot.DependsOn, out _, invComp))
                {
                    _inventorySys.SpawnItemInSlot(player, slot.DependsOn, SlotFillerId, force: true);
                }

                if (_inventorySys.TryEquip(player, ent, slot.Name))
                {
                    equipped = true;
                    break;
                }
            }
            Assert.That(equipped, $"Failed to equip {protoId}");
        });

        // Get all available appearance options.
        IEnumerable<EntProtoId> options = default!;
        await Server.WaitAssertion(() =>
        {
            options = _chameleonClothingSys.GetValidTargets(comp.Slot, comp.RequireTag);
            Assert.That(options, Is.Not.Empty, $"{protoId} does not have any possible target appearances!");
        });

        using (Assert.EnterMultipleScope())
        {
            // Try to activate each appearance option.
            foreach (var option in options)
            {
                TestContext.Out.WriteLine($"Testing option {option}");
                await Server.WaitPost(() =>
                {
                    // Activate the chameleon appearance option.
                    _chameleonClothingSys.SetSelectedPrototype(ent, option);
                });

                // Wait for the client to catch up.
                await Pair.RunUntilSynced();
            }
        }
    }
}
