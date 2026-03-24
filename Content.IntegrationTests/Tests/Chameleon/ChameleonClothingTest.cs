#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Utility;
using Content.Server.Inventory;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chameleon;

public sealed class ChameleonClothingTest : InteractionTest
{
    private static readonly string[] ChameleonClothingEntities = GameDataScrounger.EntitiesWithComponent("ChameleonClothing");

    protected override string PlayerPrototype => "MobHuman";

    private const string JumpsuitId = "ClothingJumpsuitTest";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {JumpsuitId}
  id: {JumpsuitId}
  parent: Clothing
  components:
  - type: Clothing
    slots: [innerclothing]
";

    [TestCaseSource(nameof(ChameleonClothingEntities))]
    [TestOf(typeof(SharedChameleonClothingSystem))]
    public async Task ActivateAllOptions(string protoId)
    {
        var chameleonClothingSys = Server.System<SharedChameleonClothingSystem>();
        var inventorySys = Server.System<InventorySystem>();
        var spriteSys = Client.System<SpriteSystem>();

        await Server.WaitPost(() =>
        {
            inventorySys.SpawnItemInSlot(SPlayer, "jumpsuit", JumpsuitId);
        });

        //var ent = await PlaceInHands(protoId);
        //await UseInHand();
        //Assert.That(HandSys.ActiveHandIsEmpty((SPlayer, Hands)), $"Failed to equip {protoId}");

        var ent = await Spawn(protoId);

        var comp = Comp<ChameleonClothingComponent>(ent);
        var invComp = Comp<InventoryComponent>(Player);

        await Server.WaitAssertion(() =>
        {
            var equipped = false;
            foreach (var slot in invComp.Slots)
            {
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
                var spriteComp = CEntMan.GetComponent<SpriteComponent>(ToClient(ent));
            }
        }

        await Delete(ent);
    }
}
