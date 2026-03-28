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

    private static readonly EntProtoId PlayerMob = "MobHuman";

    [SidedDependency(Side.Server)] private readonly SharedChameleonClothingSystem _chameleonClothingSys = null!;
    [SidedDependency(Side.Server)] private readonly InventorySystem _inventorySys = null!;

    // A dummy clothing entity that we can use to fill slots that other slots depend on (i.e. jumpsuit)
    private const string SlotFillerId = "ClothingDummySlotFiller";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {SlotFillerId}
  id: {SlotFillerId}
  parent: Clothing
  components:
  - type: Clothing
    slots: [all]
";

    [TestCaseSource(nameof(ChameleonClothingEntities))]
    [TestOf(typeof(SharedChameleonClothingSystem))]
    [Description("Activates all available appearance options for a chameleon clothing item.")]
    [TrackingIssue("https://github.com/space-wizards/space-station-14/issues/43365")]
    public async Task ActivateAllOptions(string protoId)
    {
        var player = await Spawn(PlayerMob);
        var ent = await Spawn(protoId);

        var comp = SComp<ChameleonClothingComponent>(ent);
        var invComp = SComp<InventoryComponent>(player);

        await Server.WaitAssertion(() =>
        {
            var equipped = false;
            foreach (var slot in invComp.Slots)
            {
                // If the slot depends on another slot being filled, stuff a dummy entity into it
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

        IEnumerable<EntProtoId>? options = default;
        await Server.WaitPost(() =>
        {
            options = _chameleonClothingSys.GetValidTargets(comp.Slot, comp.RequireTag);
        });
        Assert.That(options, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            foreach (var option in options)
            {
                TestContext.Out.WriteLine($"Testing option {option}");
                await Server.WaitPost(() =>
                {
                    // Activate the chameleon option
                    _chameleonClothingSys.SetSelectedPrototype(ent, option);
                });

                // Wait for the client to catch up.
                await Pair.RunUntilSynced();
            }
        }
    }
}
