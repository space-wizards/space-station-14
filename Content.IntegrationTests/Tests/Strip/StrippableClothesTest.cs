using System.Collections.Generic;
using System.Diagnostics;
using Content.Shared.Inventory;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using NUnit.Framework.Internal;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{

     [TestFixture]
     public sealed class StrippableClothesTest
     {
         [TestPrototypes]
         private const string Prototypes = @"
- type: entity
  name: InventoryEyesDummy
  id: InventoryEyesDummy
  components:
  - type: Clothing
    slots: eyes

- type: entity
  name: InventoryEarsDummy
  id: InventoryEarsDummy
  components:
  - type: Clothing
    slots: ears

- type: entity
  name: InventoryHeadDummy
  id: InventoryHeadDummy
  components:
  - type: Clothing
    slots: head

- type: entity
  name: InventoryNeckDummy
  id: InventoryNeckDummy
  components:
  - type: Clothing
    slots: neck

- type: entity
  name: InventoryMaskDummy
  id: InventoryMaskDummy
  components:
  - type: Clothing
    slots: mask

- type: entity
  name: InventoryShoesDummy
  id: InventoryShoesDummy
  components:
  - type: Clothing
    slots: FEET

- type: entity
  name: InventoryGlovesDummy
  id: InventoryGlovesDummy
  components:
  - type: Clothing
    slots: gloves

- type: entity
  name: InventoryFireSuitDummy
  id: InventoryFireSuitDummy
  components:
  - type: Clothing
    slots: innerclothing

- type: entity
  name: InventoryBackpackDummy
  id: InventoryBackpackDummy
  components:
  - type: Clothing
    slots: back

";
        [Test]
        public async Task StripClothesTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();
            var systemMan = sEntities.EntitySysManager;

            EntityUid target = default!;
            EntityUid attacker = default!;
            // EntityUid target = sEntities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            // EntityUid attacker = sEntities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            InventorySystem invSystem = systemMan.GetEntitySystem<InventorySystem>();

            Dictionary<string, string> clothesSlots = new Dictionary<string, string>
            {
                {"eyes", "InventoryEyesDummy"},
                {"ears", "InventoryEarsDummy"},
                {"head", "InventoryHeadDummy"},
                {"neck", "InventoryNeckDummy"},
                {"mask", "InventoryMaskDummy"},
                {"shoes", "InventoryShoesDummy"},
                {"gloves", "InventoryGlovesDummy"},
                {"outerClothing", "InventoryFireSuitDummy"},
                {"id", "InventoryIDCardDummy"},
                {"back", "InventoryBackpackDummy"},
                {"jumpsuit", "InventoryJumpsuitJanitorDummy"},
            };

            await server.WaitAssertion(() =>
            {
                target = sEntities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
                attacker = sEntities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);

                foreach (var slot in clothesSlots.Keys)
                {
                    Assert.That(invSystem.HasSlot(target, slot), Is.True);
                }

                foreach (var keyValuePair in clothesSlots)
                {
                    Assert.That(invSystem.SpawnItemInSlot(target, keyValuePair.Key, keyValuePair.Value, true, true), Is.True);
                }

                foreach (var slot in clothesSlots.Keys)
                {
                    Assert.That(invSystem.TryGetSlotEntity(target, slot, out _), Is.True);
                }

            });

            await server.WaitPost(() =>
            {
                foreach (var slot in clothesSlots.Keys)
                {
                    var ev =  new StrippingSlotButtonPressed(slot, false);
                    ev.Actor = attacker;
                    server.EntMan.EventBus.RaiseLocalEvent(target, ev);
                }

            });

            await pair.RunTicksSync(250);

            await server.WaitAssertion(() =>
            {
                foreach (var slot in clothesSlots.Keys)
                {
                    Assert.That(invSystem.TryGetSlotEntity(target, slot, out _), Is.False);
                }
            });

            await pair.CleanReturnAsync();
        }
    }
}
