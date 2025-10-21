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

    [TestPrototypes]
    private const string TestPrototypes = @"
- type: entity
  id: DummyOneHanded
  name: Urist McOneHand
  components:
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
    handsNeeded: 2

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
                Assert.That(CHands.GetHandCount(SPlayer), Is.EqualTo(hands));
                Assert.That(CHands.CountFreeHands(SPlayer), Is.EqualTo(freeHands));
                Assert.That(itemCount, Is.EqualTo(items));
                Assert.That(virtualItemCount, Is.EqualTo(virtualItems));
            });
        });
    }
}

public sealed class OneHandedItemTestFixture : BaseMultiHandedItemTest
{
    protected override string PlayerPrototype => Dummy1;

    [Test]
    public async Task OneHandedItemTest()
    {
        // We can pick up a one-handed item with one hand
        await AssertHandItems(1, 1, 0, 0);
        await SpawnTarget(Crowbar1);
        await Pickup();
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

    [Test]
    public async Task TwoHandedItemTest()
    {

    }
}
