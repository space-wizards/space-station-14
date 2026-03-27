#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Utility;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chameleon;

public sealed class ChameleonClothingTest : InteractionTest
{
    private static readonly string[] ChameleonClothingEntities = GameDataScrounger.EntitiesWithComponent("ChameleonClothing");

    protected override string PlayerPrototype => "MobHuman"; // The interaction test dummy doesn't have inventory slots, so we grab a real mob that has them.

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
    public async Task ActivateAllOptions(string protoId)
    {
        var chameleonClothingSys = Server.System<SharedChameleonClothingSystem>();
        var inventorySys = Server.System<InventorySystem>();

        var ent = await Spawn(protoId);

        var comp = Comp<ChameleonClothingComponent>(ent);
        var invComp = Comp<InventoryComponent>(Player);

        await Server.WaitAssertion(() =>
        {
            var equipped = false;
            foreach (var slot in invComp.Slots)
            {
                // If the slot depends on another slot being filled, stuff a dummy entity into it
                if (!string.IsNullOrEmpty(slot.DependsOn) && !inventorySys.TryGetSlotEntity(SPlayer, slot.DependsOn, out _, invComp))
                {
                    inventorySys.SpawnItemInSlot(SPlayer, slot.DependsOn, SlotFillerId, force: true);
                }

                if (inventorySys.TryEquip(SPlayer, ToServer(ent), slot.Name))
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
            options = chameleonClothingSys.GetValidTargets(comp.Slot, comp.RequireTag);
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
                    chameleonClothingSys.SetSelectedPrototype(ToServer(ent), option);
                });

                // Wait for the client to catch up.
                await Pair.RunUntilSynced();
            }
        }

        await Delete(ent);
    }
}
