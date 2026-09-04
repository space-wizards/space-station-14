using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
public sealed class HandForceDropTests: InteractionTest
{

#region prototypes
    private const string Crowbar = "Crowbar";
    private const string UnremovableCrowbar = "UnremovableCrowbar";
    private const string UnremovableDeleteOnDropCrowbar = "UnremovableDeleteOnDropCrowbar";
    
    private const string TestBox = "TestBox";
    private const string TestBoxLimitedSpace = "TestBoxLimitedSpace";
    
    [TestPrototypes] internal const string Prototypes = $@"
#- type: entity
#  parent: BaseItem
#  id: {Crowbar}
#  components:
#  - type: Sprite
#    sprite: Objects/Tools/crowbar.rsi
#    state: icon

- type: entity
  parent: {Crowbar}
  id: {UnremovableCrowbar}
  components:
  - type: Unremoveable
    deleteOnDrop: false

- type: entity
  parent: {Crowbar}
  id: {UnremovableDeleteOnDropCrowbar}
  components:
  - type: Unremoveable
    deleteOnDrop: true

- type: entity
  id: {TestBox}
  name: box
  components:
  - type: EntityStorage
  - type: ContainerContainer
    containers:
      entity_storage: !type:Container

- type: entity
  id: {TestBoxLimitedSpace}
  name: box
  components:
  - type: EntityStorage
    open: true
    isCollidableWhenOpen: false
    capacity: 1
  - type: ContainerContainer
    containers:
      entity_storage: !type:Container
";
#endregion

    [Test]
    public async Task TestTryDropUnremovable()
    {
        var initialFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(initialFreeHands, Is.GreaterThan(0), "SPlayer must have at least one free hand");

        var item = SEntMan.GetEntity(await PlaceInHands(UnremovableCrowbar));
        
        var holdingItemFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(holdingItemFreeHands, Is.LessThan(initialFreeHands), "SPlayer somehow has more free hands after being given the item");
        
        Assert.That(HandSys.TryDrop(SPlayer, force: false), Is.False, "SPlayer was somehow able to drop the unremovable item without it being forced");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(holdingItemFreeHands), "SPlayer somehow freed a hand after unforced drop of unremovable item.");
        Assert.That(HandSys.IsHeld(item, out _), Is.True, "Unremovable item was no longer held after unforced drop (should still be held)");
        
        Assert.That(HandSys.TryDrop(SPlayer, force:true), Is.True, "SPlayer was unable to drop the item despite being forced to drop it");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(initialFreeHands), "SPlayer did not return to initial free hand count after forcibly dropping the item!");
        Assert.That(HandSys.IsHeld(item, out _), Is.False, "The item was still held after being forcibly dropped!");
    }

    [Test]
    public async Task TestTryDropUnremovable_DeleteOnDrop()
    {
        var initialFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(initialFreeHands, Is.GreaterThan(0), "SPlayer must have at least one free hand");
        
        var item = SEntMan.GetEntity(await PlaceInHands(UnremovableDeleteOnDropCrowbar));
        
        var holdingItemFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(holdingItemFreeHands, Is.LessThan(initialFreeHands), "SPlayer somehow has more free hands after being given the item");
        
        Assert.That(HandSys.TryDrop(SPlayer, force:true), Is.True, "SPlayer was unable to drop the item despite being forced to drop it");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(initialFreeHands), "SPlayer did not return to initial free hand count after forcibly dropping the item!");
        Assert.That(HandSys.IsHeld(item, out _), Is.False, "The item was still held after being forcibly dropped!");

        Assert.That(SEntMan.IsQueuedForDeletion(item), Is.True,
            "DeleteOnDrop item was not queued for deletion after being forcibly dropped!");
    }

    [Test]
    public async Task TestTryDropAllUnremovable()
    {
        Assume.That(HandSys.GetEmptyHandCount(SPlayer), Is.EqualTo(HandSys.GetHandCount(SPlayer)), "SPlayer did not start with all hands free");

        // make sure we have at least two hands
        await HandDuplicationHelper(2);

        var initialFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(initialFreeHands, Is.GreaterThan(0), "SPlayer must have at least one free hand");

        // fill hands with unremovable items
        List<EntityUid> items = new List<EntityUid>(initialFreeHands);
        while (HandSys.TryGetEmptyHand(SPlayer, out var emptyHand))
        {
            HandSys.SetActiveHand(SPlayer, emptyHand);
            items.Add(SEntMan.GetEntity(await PlaceInHands(UnremovableCrowbar)));
        }
        var holdingItemsFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(holdingItemsFreeHands, Is.LessThan(initialFreeHands), "SPlayer somehow has more free hands after being given the item");

        // attempting to drop all normally, should fail.
        HandSys.DropAll(SPlayer);
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(holdingItemsFreeHands), "SPlayer somehow freed a hand after unforced DropAll of unremovable items.");

        // attempting to force drop all, should free all hands.
        HandSys.DropAll(SPlayer, force: true);
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(initialFreeHands), "SPlayer did not free all hands after force dropping all");
        foreach (var item in items)
        {
            Assert.That(HandSys.IsHeld(item, out _), Is.False, $"Item {SToPrettyString(item)} was still held after being forcibly dropped!");
        }
    }

#region helper methods
    /// <summary>
    /// helper method which duplicates the active hand of SPlayer, so player will have at least <c>targetHandCount</c> hands.<br/>
    /// This assumes that the player has hands.<br/>
    /// Uses <see cref="Content.IntegrationTests.Tests.Interaction.InteractionTest.SPlayer">base.SPlayer</see>,
    /// <see cref="Content.IntegrationTests.Tests.Interaction.InteractionTest.HandSys">base.HandSys</see>,
    /// and <see cref="Content.IntegrationTests.Tests.Interaction.InteractionTest.Hands">base.Hands</see>
    /// </summary>
    /// <param name="targetHandCount">how many hands do we want?<br/>
    /// function will ensure player has at least this many hands.<br/>
    /// surplus hands will <b>not</b> be removed if player already has more hands than target.</param>
    private async Task HandDuplicationHelper(int targetHandCount)
    {
        Assume.That(Hands, Is.Not.Null, "SPlayer has no HandsComponent :(");
        var initialHands = HandSys.GetHandCount(SPlayer);
        if (initialHands >= targetHandCount)
            return;
        Assume.That(initialHands, Is.GreaterThan(0));
        var handName = HandSys.GetActiveHand(SPlayer);
        Assume.That(handName, Is.Not.Null, "SPlayer active hand has no ID");
        var hand = Hands!.Hands[HandSys.GetActiveHand(SPlayer)!];
        await Server.WaitPost(() =>
        {
            for (var i = initialHands; i < targetHandCount; i++)
            {
                HandSys.AddHand(SPlayer, $"{handName}_{i}", hand);
            }
        });
        await RunTicks(1);
        Assert.That(HandSys.GetHandCount(SPlayer), Is.EqualTo(targetHandCount), "Unable to update hand count to meet target hand count :(");
    }
#endregion


    /// <summary>
    /// There is an edge case in <see cref="Content.Shared.Hands.EntitySystems.SharedHandsSystem.TryDrop(Entity{HandsComponent?}, string, Robust.Shared.Map.EntityCoordinates?, bool, bool, bool)">TryDrop()</see>,
    /// involving dropping an item when the player is inside a container.<br/>
    /// This edge case is a pain in the backside to test. Testing it with a normal IntegrationTest doesn't seem to work, unable to get IntegrationTest player into a box.<br/>
    /// However, I did notice that <see cref="Content.IntegrationTests.Tests.Hands.HandTests.TestPickUpThenDropInContainer">HandTests.TestPickUpThenDropInContainer</see> does manage to test it.<br/>
    /// This class, along with its inheriting subclasses, are intended to test the <c>force=true</c> variant of TryDrop whilst the player is in a container.<br/>
    /// This particular class provides the method used for testing + other helper methods, the subclasses actually have the tests.<br/><br/>
    /// Why do the subclasses each only contain two test methods? Because for some honkmotherforsaken reason, things seem to break upon having three test methods in this class<br/>
    /// (error message complains about the player not having a HandsComponent - no idea how the hell that happens, I'm not brave enough to find out).<br/><br/>
    /// If you feel like you'd be able to write better tests for this edge case, PLEASE DO SO.
    /// </summary>
    abstract class ContainerForceDropTests : GameTest
    {
        public override PoolSettings PoolSettings => new()
        {
            Connected = true,
            DummyTicker = false
        };

        /// <summary>
        /// helper enum indicating the item type we want
        /// </summary>
        protected enum ItemType
        {
            Droppable,
            Unremovable,
            UnremovableDeleteOnDrop
        }

        /// <summary>
        /// obtains prototype ID for an item of the desired type (droppable, unremovable, unremovable with DeleteOnDrop)
        /// </summary>
        protected string GetItemProtoId(ItemType desiredType)
        {
            return desiredType switch
            {
                ItemType.Droppable => Crowbar,
                ItemType.Unremovable => UnremovableCrowbar,
                ItemType.UnremovableDeleteOnDrop => UnremovableDeleteOnDropCrowbar,
                _ => throw new ArgumentOutOfRangeException(nameof(desiredType), desiredType, null)
            };
        }

        /// <summary>
        /// provides the prototype ID of the big box if arg is true. otherwise returns prototype ID of the small box (the box with capacity of 1)
        /// </summary>
        private string GetBoxProtoId(bool bigBoxNeeded)
        {
            return bigBoxNeeded ? TestBox : TestBoxLimitedSpace;
        }

        /// <summary>
        /// A copy of <see cref="Content.IntegrationTests.Tests.Hands.HandTests.TestPickUpThenDropInContainer">HandTests.TestPickUpThenDropInContainer()</see>,
        /// but turned into a reusable test harness for the 'TryDrop' method of <see cref="Content.Shared.Hands.EntitySystems.SharedHandsSystem">SharedHandsSystem</see>
        /// for the case where the player is in a container, forced to drop an item, but the container may or may not have enough space for the item to be dropped inside it.
        /// </summary>
        /// <param name="itemType">which item do we want the player to attempt dropping?</param>
        /// <param name="bigBox">true for big box (has space for dropped item), false for small box (no space for dropped item)</param>
        /// <param name="forceDrop">will we <c>force</c> the drop?</param>
        /// <param name="expectDropToSucceed">Do we expect <c>TryDrop</c> to succeed?</param>
        /// <param name="expectDropToSucceedMessage">assert message for unexpected outcome</param>
        /// <param name="expectItemStillExists">Do we expect the item to still exist after drop attempt?</param>
        /// <param name="expectItemStillExistsMessage">assert message for unexpected outcome</param>
        /// <param name="expectDroppedItemStillInHand">Do we expect the dropped item to still be in hand?</param>
        /// <param name="expectDroppedItemStillInHandMessage">assert message for unexpected outcome</param>
        /// <param name="expectDroppedItemToBeInContainer">Do we expect the dropped item to still be in the container?</param>
        /// <param name="expectDroppedItemToBeInContainerMessage">assert message for unexpected outcome</param>
        protected async Task TestPickUpThenForceDropInContainer_Template(
            ItemType itemType,
            bool bigBox,
            bool forceDrop,
            bool expectDropToSucceed, string expectDropToSucceedMessage,
            bool expectItemStillExists, string expectItemStillExistsMessage,
            bool expectDroppedItemStillInHand, string expectDroppedItemStillInHandMessage,
            bool expectDroppedItemToBeInContainer, string expectDroppedItemToBeInContainerMessage
        )
        {
            var pair = Pair;
            var server = pair.Server;
            var map = await pair.CreateTestMap();
            await pair.RunTicksSync(5);

            var entMan = server.ResolveDependency<IEntityManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var mapSystem = server.System<SharedMapSystem>();
            var sys = entMan.System<SharedHandsSystem>();
            var tSys = entMan.System<TransformSystem>();
            var containerSystem = server.System<SharedContainerSystem>();

            EntityUid item = default;
            EntityUid box = default;
            EntityUid player = default;
            HandsComponent hands = default!;

            // spawn the elusive box and crowbar at the coordinates
            await server.WaitPost(() => box = server.EntMan.SpawnEntity(GetBoxProtoId(bigBox), map.GridCoords));
            await server.WaitPost(() => item = server.EntMan.SpawnEntity(GetItemProtoId(itemType), map.GridCoords));
            // place the player at the exact same coordinates and have them grab the crowbar
            await server.WaitPost(() =>
            {
                player = playerMan.Sessions.First().AttachedEntity!.Value;
                tSys.PlaceNextTo(player, item);
                hands = entMan.GetComponent<HandsComponent>(player);
                sys.TryPickup(player, item, hands.ActiveHandId!);
            });
            await pair.RunTicksSync(5);
            Assert.That(sys.GetActiveItem((player, hands!)), Is.EqualTo(item));

            // Open then close the box to place the player, who is holding the crowbar, inside of it
            var storage = server.System<EntityStorageSystem>();
            await server.WaitPost(() =>
            {
                storage.OpenStorage(box);
                storage.CloseStorage(box);
            });
            await pair.RunTicksSync(5);
            Assert.That(containerSystem.IsEntityInContainer(player), Is.True);

            // Attempt dropping the item whilst player is inside the box
            await server.WaitPost(() =>
            {
                Assert.That(
                    sys.TryDrop(player, item, force: forceDrop),
                    expectDropToSucceed ? Is.True : Is.False,
                    expectDropToSucceedMessage
                );
            });
            await pair.RunTicksSync(5);

            Assert.That(
                entMan.EntityExists(item),
                expectItemStillExists ? Is.True : Is.False,
                expectItemStillExistsMessage
            );
            // only work out where the item is if we still expect it to exist.
            if (expectItemStillExists)
            {
                Assert.That(
                    sys.GetActiveItem((player, hands)),
                    expectDroppedItemStillInHand ? Is.EqualTo(item) : Is.Not.EqualTo(item),
                    expectDroppedItemStillInHandMessage
                );
                if (expectDropToSucceed) // only need to check if the item is in the container if we expected the drop to succeed
                {
                    var xform = entMan.GetComponent<TransformComponent>(player);
                    var itemXform = entMan.GetComponent<TransformComponent>(item);
                    Assert.That(
                        containerSystem.IsInSameOrNoContainer((player, xform), (item, itemXform)),
                        expectDroppedItemToBeInContainer ? Is.True : Is.False,
                        expectDroppedItemToBeInContainerMessage
                    );
                }
            }

            await server.WaitPost(() => mapSystem.DeleteMap(map.MapId));
        }
    }

    /// <summary>
    /// probably a bit redundant considering that <see cref="Content.IntegrationTests.Tests.Hands.HandTests.TestPickUpThenDropInContainer">HandTests.TestPickUpThenDropInContainer</see>
    /// tests the same thing.<br/>
    /// but at least it proves that the method works i guess
    /// </summary>
    [TestFixture]
    sealed class TestForceDropInContainerNormal: ContainerForceDropTests
    {
        [Test]
        public async Task TestPickUpThenDropInContainer_NormalItem_HasCapacity()
        {
            await TestPickUpThenForceDropInContainer_Template(
                ItemType.Droppable,
                bigBox: true,
                forceDrop: false,
                expectDropToSucceed: true,
                expectDropToSucceedMessage: "Somehow unable to drop regular item",
                expectDroppedItemStillInHand: false,
                expectDroppedItemStillInHandMessage: "Regular item somehow still in hands after being dropped",
                expectItemStillExists: true,
                expectItemStillExistsMessage:"Regular item somehow ceased to exist after being dropped",
                expectDroppedItemToBeInContainer: true,
                expectDroppedItemToBeInContainerMessage: "Regular item somehow not in container after being dropped"
            );
        }
        
        [Test]
        public async Task TestPickUpThenDropInContainer_NormalItem_NoCapacity()
        {
            await TestPickUpThenForceDropInContainer_Template(
                ItemType.Droppable,
                bigBox: false,
                forceDrop: false,
                expectDropToSucceed: true,
                expectDropToSucceedMessage: "Somehow unable to drop regular item",
                expectDroppedItemStillInHand: false,
                expectDroppedItemStillInHandMessage: "Regular item somehow still in hands after being dropped",
                expectItemStillExists: true,
                expectItemStillExistsMessage:"Regular item somehow ceased to exist after being dropped",
                expectDroppedItemToBeInContainer: true,
                expectDroppedItemToBeInContainerMessage: "Regular item somehow not in container after being dropped"
            );
        }
    }

    [TestFixture]
    sealed class TestForceDropInContainerUnremovable: ContainerForceDropTests
    {        
        /// <summary>
        /// force-dropping unremovable item whilst in a container which has space for the item.<br/>
        /// expected outcome: item gets dropped, and ends up inside the same container as the player.
        /// </summary>
        [Test]
        public async Task TestPickUpThenDropInContainer_UnremovableItem_HasCapacity()
        {
            await TestPickUpThenForceDropInContainer_Template(
                ItemType.Unremovable,
                bigBox: true,
                forceDrop: true,
                expectDropToSucceed: true,
                expectDropToSucceedMessage: "Somehow unable to force-drop unremovable item",
                expectDroppedItemStillInHand: false,
                expectDroppedItemStillInHandMessage: "force-dropped unremovable item somehow still in hands after being dropped",
                expectItemStillExists: true,
                expectItemStillExistsMessage:"force-dropped unremovable item somehow ceased to exist after being dropped",
                expectDroppedItemToBeInContainer: true,
                expectDroppedItemToBeInContainerMessage: "force-dropped unremovable item somehow not in container after being dropped"
            );
        }
        
        /// <summary>
        /// force-dropping unremovable item whilst in a container which DOES NOT have space for items.<br/>
        /// expected outcome: item gets dropped, and ends up on the ground outside the container
        /// </summary>
        [Test]
        public async Task TestPickUpThenDropInContainer_UnremovableItem_NoCapacity()
        {
            
            await TestPickUpThenForceDropInContainer_Template(
               ItemType.Unremovable,
               bigBox: false,
               forceDrop: true,
               expectDropToSucceed: true,
               expectDropToSucceedMessage: "Somehow unable to force-drop unremovable item",
               expectDroppedItemStillInHand: false,
               expectDroppedItemStillInHandMessage: "force-dropped unremovable item somehow still in hands after being dropped",
               expectItemStillExists: true,
               expectItemStillExistsMessage:"force-dropped unremovable item somehow ceased to exist after being dropped",
               expectDroppedItemToBeInContainer: false,
               expectDroppedItemToBeInContainerMessage: "force-dropped unremovable item is in the container but there's no space in the container"
           );
        }
    }

    [TestFixture]
    sealed class TestForceDropInContainerUnremovableDeleteOnDrop: ContainerForceDropTests
    {        
        /// <summary>
        /// force-dropping unremovable delete-on-drop item whilst in a container which DOES NOT have space for items.<br/>
        /// expected outcome: item removed from hands, and is deleted.
        /// </summary>
        [Test]
        public async Task TestPickUpThenDropInContainer_UnremovableItemDeleteOnDrop_HasCapacity()
        {
            await TestPickUpThenForceDropInContainer_Template(
                ItemType.UnremovableDeleteOnDrop,
                bigBox: true,
                forceDrop: true,
                expectDropToSucceed: true,
                expectDropToSucceedMessage: "Somehow unable to force-drop unremovable delete-on-drop item",
                expectDroppedItemStillInHand: false,
                expectDroppedItemStillInHandMessage: "force-dropped unremovable delete-on-drop item item somehow still in hands after being dropped",
                expectItemStillExists: false,
                expectItemStillExistsMessage:"force-dropped unremovable delete-on-drop item was not deleted after being dropped",
                expectDroppedItemToBeInContainer: false,
                expectDroppedItemToBeInContainerMessage: "YOU SHOULD NOT SEE THIS MESSAGE (delete-on-drop was dropped and is present in container)"
            );
        }
        
        /// <summary>
        /// force-dropping unremovable delete-on-drop item whilst in a container which DOES NOT have space for items.<br/>
        /// expected outcome: item removed from hands, and is deleted.
        /// </summary>
        [Test]
        public async Task TestPickUpThenDropInContainer_UnremovableItemDeleteOnDrop_NoCapacity()
        {
            await TestPickUpThenForceDropInContainer_Template(
               ItemType.UnremovableDeleteOnDrop,
               bigBox: false,
               forceDrop: true,
               expectDropToSucceed: true,
               expectDropToSucceedMessage: "Somehow unable to force-drop unremovable delete-on-drop item",
               expectDroppedItemStillInHand: false,
               expectDroppedItemStillInHandMessage: "force-dropped unremovable delete-on-drop item item somehow still in hands after being dropped",
               expectItemStillExists: false,
               expectItemStillExistsMessage:"force-dropped unremovable delete-on-drop item was not deleted after being dropped",
               expectDroppedItemToBeInContainer: false,
               expectDroppedItemToBeInContainerMessage: "YOU SHOULD NOT SEE THIS MESSAGE (delete-on-drop was dropped and is present in container but no space in container)"
           );
        }
    }

}