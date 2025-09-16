using System.Collections.Generic;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Strip
{
     public sealed class StrippableClothesTest
     {
         [TestPrototypes]
         private const string Prototypes = @"
- type: entity
  name: InventoryDummyEyes
  id: InventoryDummyEyes
  components:
  - type: Clothing
    slots: eyes

- type: entity
  name: InventoryDummyEars
  id: InventoryDummyEars
  components:
  - type: Clothing
    slots: ears

- type: entity
  name: InventoryDummyHead
  id: InventoryDummyHead
  components:
  - type: Clothing
    slots: head

- type: entity
  name: InventoryDummyNeck
  id: InventoryDummyNeck
  components:
  - type: Clothing
    slots: neck

- type: entity
  name: InventoryDummyMask
  id: InventoryDummyMask
  components:
  - type: Clothing
    slots: mask

- type: entity
  name: InventoryDummyShoes
  id: InventoryDummyShoes
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
  name: InventoryDummyFireSuit
  id: InventoryDummyFireSuit
  components:
  - type: Clothing
    slots: innerclothing

- type: entity
  name: InventoryDummyBackpack
  id: InventoryDummyBackpack
  components:
  - type: Clothing
    slots: back

- type: entity
  name: DummyHandItem
  id: DummyHandItem
  parent: BaseItem
  components:
  - type: Item

";

         private readonly EntProtoId _humanMob = new EntProtoId("MobHuman");
         private readonly EntProtoId _handItem = new EntProtoId("DummyHandItem");

         private const string LeftHandSlot = "body_part_slot_left_hand";
         private const string RightHandSlot = "body_part_slot_right_hand";

         private const int TotalHandSlotsCount = 2;


        [Test]
        //  Test case for validating stripping clothes for target humanMob
        public async Task StripClothesMainTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid target = default!;
            EntityUid attacker = default!;

            InventorySystem invSystem = sEntities.System<InventorySystem>();

            Dictionary<string, string> clothesSlots = new Dictionary<string, string>
            {
                {"eyes", "InventoryDummyEyes"},
                {"ears", "InventoryDummyEars"},
                {"head", "InventoryDummyHead"},
                {"neck", "InventoryDummyNeck"},
                {"mask", "InventoryDummyMask"},
                {"shoes", "InventoryDummyShoes"},
                {"gloves", "InventoryGlovesDummy"},
                {"outerClothing", "InventoryDummyFireSuit"},
                {"id", "InventoryIDCardDummy"},
                {"back", "InventoryDummyBackpack"},
                {"jumpsuit", "InventoryJumpsuitJanitorDummy"},
            };

            await server.WaitAssertion(() =>
            {
                target = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);
                attacker = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);

                //  Checking for valid slots
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.HasSlot(target, slot),
                            Is.True,
                            $"{_humanMob} does not have a slot {slot}"
                            );
                    }
                });

                //  Spawning clothes in slots
                Assert.Multiple(() =>
                {
                    foreach (var keyValuePair in clothesSlots)
                    {
                        Assert.That(
                            invSystem.SpawnItemInSlot(target, keyValuePair.Key, keyValuePair.Value, true, true),
                            Is.True,
                            $"Could not spawn {keyValuePair.Value} in slot {keyValuePair.Key}"
                            );
                    }
                });

                //  All slots must be filled with prototypes
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.True,
                            $"{_humanMob} does not have an equipped item in slot {slot}"
                            );
                    }
                });
            });

            //  Calling stripping event
            await server.WaitPost(() =>
            {
                foreach (var slot in clothesSlots.Keys)
                {
                    var ev =  new StrippingSlotButtonPressed(slot, false);
                    ev.Actor = attacker;
                    server.EntMan.EventBus.RaiseLocalEvent(target, ev);
                }
            });

            //  Time to wait doAfter event
            await pair.RunSeconds(15);

            //  Validating that slots should be stripped and empty
            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.False,
                            $"{slot} slot should be empty because of stripping"
                        );
                    }
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        // Stripping test when attacker humanMob have both hands full
        public async Task StripWithFullHandsTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid target = default!;
            EntityUid attacker = default!;

            InventorySystem invSystem = sEntities.System<InventorySystem>();

            EntityUid[] items = new EntityUid[TotalHandSlotsCount];

            SharedHandsSystem handsSystem = sEntities.System<SharedHandsSystem>();

            string[] handsSlots =
            [
                LeftHandSlot,
                RightHandSlot,
            ];

            Dictionary<string, string> clothesSlots = new Dictionary<string, string>
            {
                {"eyes", "InventoryDummyEyes"},
                {"ears", "InventoryDummyEars"},
                {"head", "InventoryDummyHead"},
                {"neck", "InventoryDummyNeck"},
                {"mask", "InventoryDummyMask"},
                {"shoes", "InventoryDummyShoes"},
                {"gloves", "InventoryGlovesDummy"},
                {"outerClothing", "InventoryDummyFireSuit"},
                {"id", "InventoryIDCardDummy"},
                {"back", "InventoryDummyBackpack"},
                {"jumpsuit", "InventoryJumpsuitJanitorDummy"},
            };

            await server.WaitAssertion(() =>
            {
                target = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);
                attacker = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);

                //  Spawn human mobs
                for (int i = 0; i < TotalHandSlotsCount; i++)
                {
                    items[i] = sEntities.SpawnEntity(_handItem, MapCoordinates.Nullspace);
                }

                //  Verifying human mob for hand slots
                Assert.Multiple(() =>
                {
                    foreach (var hand in handsSlots)
                    {
                        Assert.That(
                            handsSystem.TryGetHand(attacker, hand, out _),
                            Is.True,
                            $"{_humanMob} does not have hand {hand}"
                            );
                    }
                });

                //  Fulling hand slots
                Assert.Multiple(() =>
                {
                    for (int i = 0; i < TotalHandSlotsCount; i++)
                    {
                        Assert.That(
                            handsSystem.TryPickup(attacker, items[i], handsSlots[i]),
                            Is.True,
                            $"Could not take item in hand {handsSlots[i]}"
                            );
                    }
                });

                //  Validating that hand slots are fulled
                Assert.Multiple(() =>
                {
                    foreach (var slot in handsSlots)
                    {
                        Assert.That(
                            handsSystem.TryGetHeldItem(attacker, slot, out _),
                            Is.True,
                            $"Hand slot {slot} is not equipped"
                        );
                    }
                });

                //  Checking for valid slots
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.HasSlot(target, slot),
                            Is.True,
                            $"{_humanMob} does not have a slot {slot}"
                        );
                    }
                });

                //  Spawning clothes in slots
                Assert.Multiple(() =>
                {
                    foreach (var keyValuePair in clothesSlots)
                    {
                        Assert.That(
                            invSystem.SpawnItemInSlot(target, keyValuePair.Key, keyValuePair.Value, true, true),
                            Is.True,
                            $"Could not spawn {keyValuePair.Value} in slot {keyValuePair.Key}"
                        );
                    }
                });

                //  All slots must be filled with prototypes
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.True,
                            $"{_humanMob} does not have an equipped item in slot {slot}"
                        );
                    }
                });
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

            //  Time to wait doAfter event
            await pair.RunSeconds(15);

            //  Validating that slots should be stripped and empty
            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.False,
                            $"{slot} slot should be empty because of stripping"
                        );
                    }
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        //  Test when target humanMob clothing slots are empty
        public async Task StripAttemptEmptySlotTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid target = default!;
            EntityUid attacker = default!;

            InventorySystem invSystem = sEntities.System<InventorySystem>();

            Dictionary<string, string> clothesSlots = new Dictionary<string, string>
            {
                {"eyes", "InventoryDummyEyes"},
                {"ears", "InventoryDummyEars"},
                {"head", "InventoryDummyHead"},
                {"neck", "InventoryDummyNeck"},
                {"mask", "InventoryDummyMask"},
                {"shoes", "InventoryDummyShoes"},
                {"gloves", "InventoryGlovesDummy"},
                {"outerClothing", "InventoryDummyFireSuit"},
                {"id", "InventoryIDCardDummy"},
                {"back", "InventoryDummyBackpack"},
                {"jumpsuit", "InventoryJumpsuitJanitorDummy"},
            };

            await server.WaitAssertion(() =>
            {
                target = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);
                attacker = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);

                //  Checking for valid slots
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.HasSlot(target, slot),
                            Is.True,
                            $"{_humanMob} does not have a slot {slot}"
                        );
                    }
                });

                //  Validating that clothing slots are empty
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.False,
                            $"Slot {slot} must be empty"
                            );
                    }
                });
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

            //  Time to wait doAfter event
            await pair.RunSeconds(15);

            //  Validating that slots should be stripped and empty
            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.False,
                            $"{slot} slot should be empty because of stripping"
                        );
                    }
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        //  Test for dependent clothing slots
        //  For this test using jumpsuit slot that should drop id after stripping
        public async Task StripDependentClothesTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid target = default!;
            EntityUid attacker = default!;

            InventorySystem invSystem = sEntities.System<InventorySystem>();

            Dictionary<string, string> clothesSlots = new Dictionary<string, string>
            {
                {"jumpsuit", "InventoryJumpsuitJanitorDummy"},
                {"id", "InventoryIDCardDummy"},
            };

            await server.WaitAssertion(() =>
            {
                target = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);
                attacker = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);

                //  Checking for valid slots
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.HasSlot(target, slot),
                            Is.True,
                            $"{_humanMob} does not have a slot {slot}"
                        );
                    }
                });

                //  Spawning clothes in slots
                Assert.Multiple(() =>
                {
                    foreach (var keyValuePair in clothesSlots)
                    {
                        Assert.That(
                            invSystem.SpawnItemInSlot(target, keyValuePair.Key, keyValuePair.Value, true, true),
                            Is.True,
                            $"Could not spawn {keyValuePair.Value} in slot {keyValuePair.Key}"
                        );
                    }
                });

                //  All slots must be filled with prototypes
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.True,
                            $"{_humanMob} does not have an equipped item in slot {slot}"
                        );
                    }
                });
            });
            await server.WaitPost(() =>
            {
                var ev =  new StrippingSlotButtonPressed("jumpsuit", false);
                ev.Actor = attacker;
                server.EntMan.EventBus.RaiseLocalEvent(target, ev);
            });

            //  Time to wait doAfter event
            await pair.RunSeconds(15);

            //  Validating that slots should be stripped and empty
            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var slot in clothesSlots.Keys)
                    {
                        Assert.That(
                            invSystem.TryGetSlotEntity(target, slot, out _),
                            Is.False,
                            $"{slot} slot should be empty because of stripping"
                        );
                    }
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        //  Test for stripping target items from hand slots
        public async Task StripItemsFromTargetHands()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid target = default!;
            EntityUid attacker = default!;

            EntityUid[] items = new EntityUid[TotalHandSlotsCount];

            SharedHandsSystem handsSystem = sEntities.System<SharedHandsSystem>();

            string[] handsSlots =
            [
                LeftHandSlot,
                RightHandSlot,
            ];

            await server.WaitAssertion(() =>
            {
                target = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);
                attacker = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);

                for (int i = 0; i < TotalHandSlotsCount; i++)
                {
                    items[i] = sEntities.SpawnEntity(_handItem, MapCoordinates.Nullspace);
                }

                //  Verifying human mob for hand slots
                Assert.Multiple(() =>
                {
                    foreach (var hand in handsSlots)
                    {
                        Assert.That(
                            handsSystem.TryGetHand(target, hand, out _),
                            Is.True,
                            $"{_humanMob} does not have hand {hand}"
                        );
                    }
                });

                //  Fulling hand slots
                Assert.Multiple(() =>
                {
                    for (int i = 0; i < TotalHandSlotsCount; i++)
                    {
                        Assert.That(
                            handsSystem.TryPickup(target, items[i], handsSlots[i]),
                            Is.True,
                            $"Could not take item in hand {handsSlots[i]}"
                        );
                    }
                });

                //  Validating that hand slots are fulled
                Assert.Multiple(() =>
                {
                    foreach (var slot in handsSlots)
                    {
                        Assert.That(
                            handsSystem.TryGetHeldItem(target, slot, out _),
                            Is.True,
                            $"Hand slot {slot} is not equipped"
                            );
                    }
                });

            });

            await server.WaitPost(() =>
            {
                foreach (var slot in handsSlots)
                {
                    var ev =  new StrippingSlotButtonPressed(slot, true);
                    ev.Actor = attacker;
                    server.EntMan.EventBus.RaiseLocalEvent(target, ev);
                }

            });

            await pair.RunSeconds(15);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var slot in handsSlots)
                    {
                        Assert.That(
                            handsSystem.TryGetHeldItem(target, slot, out _),
                            Is.False,
                            $"Hand slot {slot} must not have item equipped"
                        );
                    }
                });
            });

            await pair.CleanReturnAsync();
        }
    }
}
