using System.Collections.Generic;
using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Strip;

public sealed class StrippableClothesTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: InventoryDummyEyes
  id: InventoryDummyEyes
  components:
  - type: Clothing
    slots: EYES

- type: entity
  name: InventoryDummyEars
  id: InventoryDummyEars
  components:
  - type: Clothing
    slots: EARS

- type: entity
  name: InventoryDummyHead
  id: InventoryDummyHead
  components:
  - type: Clothing
    slots: HEAD

- type: entity
  name: InventoryDummyNeck
  id: InventoryDummyNeck
  components:
  - type: Clothing
    slots: NECK

- type: entity
  name: InventoryDummyMask
  id: InventoryDummyMask
  components:
  - type: Clothing
    slots: MASK

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
    slots: GLOVES

- type: entity
  name: InventoryDummyFireSuit
  id: InventoryDummyFireSuit
  components:
  - type: Clothing
    slots: INNERCLOTHING

- type: entity
  name: InventoryDummyBackpack
  id: InventoryDummyBackpack
  components:
  - type: Clothing
    slots: BACK

- type: entity
  name: DummyHandItem
  id: DummyHandItem
  parent: BaseItem
  components:
  - type: Item

";

    private readonly EntProtoId _humanMob = new EntProtoId("MobHuman");
    private const int TotalHandsCount = 2;
    private const int RunSecondsDoAfterEvent = 15;

    [Test]
    // <summary>
    //  Test case for validating stripping clothes for target humanMob
    // </summary>
    public async Task StripClothesMainTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        EntityUid target = default!;
        EntityUid attacker = default!;

        var invSystem = sEntities.System<InventorySystem>();

        var clothesSlots = new Dictionary<string, string>
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
        await pair.RunSeconds(RunSecondsDoAfterEvent);

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
    // <summary>
    // Stripping test when attacker humanMob have both hands full
    // </summary>
    public async Task StripWithFullHandsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        EntityUid target = default!;
        EntityUid attacker = default!;

        var invSystem = sEntities.System<InventorySystem>();

        var handsSystem = sEntities.System<SharedHandsSystem>();

        var items = new List<EntityUid>();

        var clothesSlots = new Dictionary<string, string>
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
            for (int i = 0; i < TotalHandsCount; i++)
            {
                items.Add(sEntities.SpawnEntity("DummyHandItem", MapCoordinates.Nullspace));
            }

            //  Verifying human mob for hand slots
            Assert.Multiple(() =>
            {
                foreach (var hand in handsSystem.EnumerateHands(attacker))
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
                foreach( var (slot, item) in handsSystem.EnumerateHands(attacker).Zip(items) )
                {
                    Assert.That(
                        handsSystem.TryPickup(attacker, item, slot),
                        Is.True,
                        $"Could not take item in hand {slot}"
                    );
                }
            });

            //  Validating that hand slots are fulled
            Assert.Multiple(() =>
            {
                foreach (var slot in handsSystem.EnumerateHands(attacker))
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
        await pair.RunSeconds(RunSecondsDoAfterEvent);

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
    // <summary>
    //  Test when target humanMob clothing slots are empty
    // </summary>
    public async Task StripAttemptEmptySlotTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        EntityUid target = default!;
        EntityUid attacker = default!;

        var invSystem = sEntities.System<InventorySystem>();

        var clothesSlots = new Dictionary<string, string>
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
        await pair.RunSeconds(RunSecondsDoAfterEvent);

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
    // <summary>
    //  Test for dependent clothing slots
    //  For this test using jumpsuit slot that should drop id after stripping
    // </summary>
    public async Task StripDependentClothesTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        EntityUid target = default!;
        EntityUid attacker = default!;

        var invSystem = sEntities.System<InventorySystem>();

        var clothesSlots = new Dictionary<string, string>
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
            var ev = new StrippingSlotButtonPressed("jumpsuit", false);
            ev.Actor = attacker;
            server.EntMan.EventBus.RaiseLocalEvent(target, ev);
        });

        //  Time to wait doAfter event
        await pair.RunSeconds(RunSecondsDoAfterEvent);

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
    // <summary>
    //  Test for stripping target items from hand slots
    // </summary>
    public async Task StripItemsFromTargetHandsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        EntityUid target = default!;
        EntityUid attacker = default!;

        var handsSystem = sEntities.System<SharedHandsSystem>();

        var items = new List<EntityUid>();

        await server.WaitAssertion(() =>
        {
            target = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);
            attacker = sEntities.SpawnEntity(_humanMob, MapCoordinates.Nullspace);

            for (int i = 0; i < TotalHandsCount; i++)
            {
                items.Add(sEntities.SpawnEntity("DummyHandItem", MapCoordinates.Nullspace));
            }

            //  Verifying human mob for hand slots
            Assert.Multiple(() =>
            {
                foreach (var hand in handsSystem.EnumerateHands(target))
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
                foreach( var (slot, item) in handsSystem.EnumerateHands(target).Zip(items) )
                {
                    Assert.That(
                        handsSystem.TryPickup(target, item, slot),
                        Is.True,
                        $"Could not take item in hand {slot}"
                    );
                }
            });

            //  Validating that hand slots are fulled
            Assert.Multiple(() =>
            {
                foreach (var slot in handsSystem.EnumerateHands(target))
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
            foreach (var slot in handsSystem.EnumerateHands(target))
            {
                var ev = new StrippingSlotButtonPressed(slot, true);
                ev.Actor = attacker;
                server.EntMan.EventBus.RaiseLocalEvent(target, ev);
            }

        });

        await pair.RunSeconds(RunSecondsDoAfterEvent);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var slot in handsSystem.EnumerateHands(target))
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
