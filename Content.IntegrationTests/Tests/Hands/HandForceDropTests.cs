using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
public sealed class HandForceDropTests: InteractionTest
{

    private const string Crowbar1 = "Crowbar1";
    private const string UnremovableCrowbar = "UnremovableCrowbar";
    private const string UnremovableDeleteOnDropCrowbar = "UnremovableDeleteOnDropCrowbar";
    
    [TestPrototypes] private const string Prototypes = $@"
- type: entity
  parent: BaseItem
  id: {Crowbar1}
  components:
  - type: Sprite
    sprite: Objects/Tools/crowbar.rsi
    state: icon

- type: entity
  parent: {Crowbar1}
  id: {UnremovableCrowbar}
  components:
  - type: Unremoveable
    deleteOnDrop: false

- type: entity
  parent: {Crowbar1}
  id: {UnremovableDeleteOnDropCrowbar}
  components:
  - type: Unremoveable
    deleteOnDrop: true
";

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
    
}