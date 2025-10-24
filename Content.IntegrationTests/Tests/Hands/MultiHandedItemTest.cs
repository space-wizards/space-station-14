using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.IntegrationTests.Tests.Hands;

[TestOf(typeof(MultiHandedItemComponent))]
[TestOf(typeof(WieldableComponent))]
public abstract class BaseMultiHandedItemTest : InteractionTest
{
    protected SharedHandsSystem CHands = default!;
    protected SharedHandsSystem SHands = default!;
    protected SharedWieldableSystem SWieldable = default!;

    protected const string Dummy1 = "DummyOneHanded";
    protected const string Dummy2 = "DummyTwoHanded";
    protected const string Dummy3 = "DummyThreeHanded";
    protected const string Dummy4 = "DummyFourHanded";
    protected const string Crowbar1 = "CrowbarOneHanded";
    protected const string Crowbar2 = "CrowbarTwoHanded";
    protected const string Crowbar3 = "CrowbarThreeHanded";
    protected const string CrowbarWield1 = "CrowbarWieldableOneHanded";
    protected const string CrowbarWield2 = "CrowbarWieldableTwoHanded";
    protected const string CrowbarWield3 = "CrowbarWieldableThreeHanded";

    [TestPrototypes]
    private const string TestPrototypes = @"
- type: entity
  id: DummyOneHanded
  name: Urist McOneHand
  components:
  - type: DoAfter
  - type: Hands
    hands:
      hand_right:
        location: Right
    sortedHands:
    - hand_right
  - type: ComplexInteraction
  - type: MindContainer
  - type: Puller

- type: entity
  parent: DummyOneHanded
  id: DummyTwoHanded
  name: Urist McTwoHands
  components:
  - type: Hands
    hands:
      hand_right:
        location: Right
      hand_left:
        location: Left
    sortedHands:
    - hand_right
    - hand_left

- type: entity
  parent: DummyTwoHanded
  id: DummyThreeHanded
  name: Urist McThreeHands
  components:
  - type: Hands
    hands:
      hand_right:
        location: Right
      hand_middle:
        location: Middle
      hand_left:
        location: Left
    sortedHands:
    - hand_right
    - hand_middle
    - hand_left

- type: entity
  parent: DummyTwoHanded
  id: DummyFourHanded
  name: Urist McFourHands
  components:
  - type: Hands
    hands:
      hand_right1:
        location: Right
      hand_right2:
        location: Right
      hand_left1:
        location: Left
      hand_left2:
        location: Left
    sortedHands:
    - hand_right1
    - hand_right2
    - hand_left1
    - hand_left2

- type: entity
  parent: BaseItem
  id: CrowbarOneHanded
  components:
  - type: Sprite
    sprite: Objects/Tools/crowbar.rsi
    state: icon

- type: entity
  parent: CrowbarOneHanded
  id: CrowbarTwoHanded
  components:
  - type: MultiHandedItem
    handsNeeded: 2

- type: entity
  parent: CrowbarOneHanded
  id: CrowbarThreeHanded
  components:
  - type: MultiHandedItem
    handsNeeded: 3

- type: entity
  parent: CrowbarOneHanded
  id: CrowbarWieldableOneHanded
  components:
  - type: Wieldable
    freeHandsRequired: 0

- type: entity
  parent: CrowbarOneHanded
  id: CrowbarWieldableTwoHanded
  components:
  - type: Wieldable
    freeHandsRequired: 1

- type: entity
  parent: CrowbarOneHanded
  id: CrowbarWieldableThreeHanded
  components:
  - type: Wieldable
    freeHandsRequired: 2
";

    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();

        CHands = CEntMan.System<SharedHandsSystem>();
        SHands = SEntMan.System<SharedHandsSystem>();
        SWieldable = SEntMan.System<SharedWieldableSystem>();
    }

    protected async Task AssertHandItems(int hands, int freeHands, int items, int virtualItems)
    {
        await RunTicks(3);
        await Server.WaitAssertion(() =>
        {

            var itemCount = 0;
            var virtualItemCount = 0;

            foreach (var item in SHands.EnumerateHeld(SPlayer))
            {
                itemCount++;
                if (SEntMan.HasComponent<VirtualItemComponent>(item))
                    virtualItemCount++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(SHands.GetHandCount(SPlayer), Is.EqualTo(hands));
                Assert.That(SHands.CountFreeHands(SPlayer), Is.EqualTo(freeHands));
                Assert.That(itemCount, Is.EqualTo(items));
                Assert.That(virtualItemCount, Is.EqualTo(virtualItems));
            });
        });
        await Client.WaitAssertion(() =>
        {
            var itemCount = 0;
            var virtualItemCount = 0;

            foreach (var item in CHands.EnumerateHeld(CPlayer))
            {
                itemCount++;
                if (CEntMan.HasComponent<VirtualItemComponent>(item))
                    virtualItemCount++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(CHands.GetHandCount(CPlayer), Is.EqualTo(hands));
                Assert.That(CHands.CountFreeHands(CPlayer), Is.EqualTo(freeHands));
                Assert.That(itemCount, Is.EqualTo(items));
                Assert.That(virtualItemCount, Is.EqualTo(virtualItems));
            });
        });
    }
}

// we need a separate fixture for each hand amount so that we can override the PlayerPrototype
public sealed class OneHandedItemTestFixture : BaseMultiHandedItemTest
{
    protected override string PlayerPrototype => Dummy1;

    /// <summary>
    /// Tries out a few possible combinations for using multi-handed and wieldable items while having one hand.
    /// This does not cover all possible scenarios, so if something breaks at some point then add it here as well.
    /// </summary>
    [Test]
    public async Task OneHandedItemTest()
    {
        // Start with one empty hand
        await AssertHandItems(1, 1, 0, 0);

        // We can pick up a one-handed item with one hand
        await SpawnTarget(Crowbar1);
        await Pickup();
        await SwapHands(); // does nothing but we try anyways
        await AssertHandItems(1, 0, 1, 0);

        // We cannot pick up a second item
        await SpawnTarget(Crowbar1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.False);
        });
        await AssertHandItems(1, 0, 1, 0);

        // We can drop our current item to free up the hand
        await Drop();
        await AssertHandItems(1, 1, 0, 0);

        // We cannot pick up a multi-handed item
        await SpawnTarget(Crowbar2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.False);
        });
        await AssertHandItems(1, 1, 0, 0);

        // Spawn a wieldable Crowbar that does not need extra hands, pick it up, wield it
        await SpawnTarget(CrowbarWield1);
        await Pickup();
        await AssertHandItems(1, 0, 1, 0);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        await AssertHandItems(1, 0, 1, 0);

        // Drop the wielded item
        await Drop();
        await AssertHandItems(1, 1, 0, 0);

        // Spawn a wieldable Crowbar that needs extra hands, pick it up, fail to wield it
        await SpawnTarget(CrowbarWield2);
        await Pickup();
        await AssertHandItems(1, 0, 1, 0);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.False);
        });
        await AssertHandItems(1, 0, 1, 0);

        // Drop the unwielded item
        await Drop();
        await AssertHandItems(1, 1, 0, 0);
    }
}

public sealed class TwoHandedItemTestFixture : BaseMultiHandedItemTest
{
    protected override string PlayerPrototype => Dummy2;

    /// <summary>
    /// Tries out a few possible combinations for using multi-handed and wieldable items while having two hands.
    /// This does not cover all possible scenarios, so if something breaks at some point then add it here as well.
    /// </summary>
    [Test]
    public async Task TwoHandedItemTest()
    {
        // Start with two empty hands
        await AssertHandItems(2, 2, 0, 0);

        // We can pick up a one-handed item with one hand
        await SpawnTarget(Crowbar1);
        await Pickup();
        await SwapHands();
        await AssertHandItems(2, 1, 1, 0);

        // We can pick up another one-handed item with the second hand
        await SpawnTarget(Crowbar1);
        await Pickup();
        await SwapHands();
        await AssertHandItems(2, 0, 2, 0);

        // We cannot pick up a third item
        await SpawnTarget(Crowbar1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.False);
        });
        await AssertHandItems(2, 0, 2, 0);

        // Drop all items
        await DropAll();
        await AssertHandItems(2, 2, 0, 0);

        // We can pick up a two-handed item with two hands
        await SpawnTarget(Crowbar2);
        await Pickup();
        await SwapHands();
        await AssertHandItems(2, 0, 2, 1);

        // We cannot pick up a second item
        await SpawnTarget(Crowbar1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.False);
        });
        await AssertHandItems(2, 0, 2, 1);

        // Drop all items
        await DropAll();
        await AssertHandItems(2, 2, 0, 0);

        // We can pick up a one-handed item with one hand
        var handOneItem = await SpawnTarget(Crowbar1);
        await Pickup();
        await SwapHands();
        await AssertHandItems(2, 1, 1, 0);

        // We cannot pick up a two-handed item with only one free hand
        await SpawnTarget(Crowbar2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.False);
        });
        await AssertHandItems(2, 1, 1, 0);

        // We can pick up a one handed wieldable item
        await SpawnTarget(CrowbarWield1);
        await Pickup();
        await AssertHandItems(2, 0, 2, 0);

        // We can wield the one-handed wieldable item without dropping other items
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        Assert.That(SHands.IsHolding(SPlayer, ToServer(handOneItem)), Is.True);
        await AssertHandItems(2, 0, 2, 0);

        // Drop it and pick up a two-handed wieldable item
        await Drop();
        await AssertHandItems(2, 1, 1, 0);
        await SpawnTarget(CrowbarWield2);
        await Pickup();
        await AssertHandItems(2, 0, 2, 0);

        // We can wield the two-handed wieldable item, but drop the item in the other hand while doing so
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        Assert.That(SHands.IsHolding(SPlayer, ToServer(handOneItem)), Is.False);
        await AssertHandItems(2, 0, 2, 1);

        // Drop the wielded item
        await Drop();
        await AssertHandItems(2, 2, 0, 0);

        // We cannot pick up a three-handed item
        await SpawnTarget(Crowbar3);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.False);
        });
        await AssertHandItems(2, 2, 0, 0);

        // We can pick up a three-handed wieldable item
        await SpawnTarget(CrowbarWield3);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SHands.TryPickupAnyHand(SPlayer, STarget.Value), Is.True);
        });
        await AssertHandItems(2, 1, 1, 0);

        // But we cannot wield it
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.False);
        });
        await AssertHandItems(2, 1, 1, 0);
    }
}

public sealed class ThreeHandedItemTestFixture : BaseMultiHandedItemTest
{
    protected override string PlayerPrototype => Dummy3;

    /// <summary>
    /// Tries out a few possible combinations for using multi-handed and wieldable items while having three hands.
    /// This does not cover all possible scenarios, so if something breaks at some point then add it here as well.
    /// </summary>
    [Test]
    public async Task ThreeHandedItemTest()
    {
        // Start with three empty hands
        await AssertHandItems(3, 3, 0, 0);

        // We can pick up a three-handed item
        await SpawnTarget(Crowbar3);
        await Pickup();
        await AssertHandItems(3, 0, 3, 2);

        // Drop it
        await Drop();
        await AssertHandItems(3, 3, 0, 0);

        // We can pick up a two-handed wieldable item
        var handOneItem = await SpawnTarget(CrowbarWield2);
        await Pickup();
        await AssertHandItems(3, 2, 1, 0);

        // And we can wield it
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        await AssertHandItems(3, 1, 2, 1);

        // We can pick up a second two-handed wieldable item
        await SwapHands();
        await SwapHands();
        await SpawnTarget(CrowbarWield2);
        await Pickup();
        await AssertHandItems(3, 0, 3, 1);

        // And wielding it drops the first item
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        Assert.That(SHands.IsHolding(SPlayer, ToServer(handOneItem)), Is.False);
        await AssertHandItems(3, 1, 2, 1);
    }
}

public sealed class FourHandedItemTestFixture : BaseMultiHandedItemTest
{
    protected override string PlayerPrototype => Dummy4;

    /// <summary>
    /// Tries out a few possible combinations for using multi-handed and wieldable items while having four hands.
    /// This does not cover all possible scenarios, so if something breaks at some point then add it here as well.
    /// </summary>
    [Test]
    public async Task FourHandedItemTest()
    {
        // Start with four empty hands
        await AssertHandItems(4, 4, 0, 0);

        // We can wield two two-handed wieldable items at the same time
        await SpawnTarget(CrowbarWield2);
        await Pickup();
        await AssertHandItems(4, 3, 1, 0);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        await AssertHandItems(4, 2, 2, 1);
        await SwapHands();
        await SwapHands();
        await SpawnTarget(CrowbarWield2);
        await Pickup();
        await AssertHandItems(4, 1, 3, 1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SWieldable.TryWield(STarget.Value, SPlayer), Is.True);
        });
        await AssertHandItems(4, 0, 4, 2);

        // Drop the first item
        await Drop();
        await AssertHandItems(4, 2, 2, 1);

        // Drop the second item
        await SwapHands();
        await SwapHands();
        await Drop();
        await AssertHandItems(4, 4, 0, 0);
    }
}
