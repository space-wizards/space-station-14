using Content.IntegrationTests.Tests.Interaction;

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

        await PlaceInHands(UnremovableCrowbar);
        
        var holdingItemFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(holdingItemFreeHands, Is.LessThan(initialFreeHands), "SPlayer somehow has more free hands after being given the item");
        
        Assert.That(HandSys.TryDrop(SPlayer, force: false), Is.False, "SPlayer was somehow able to drop the unremovable item without it being forced");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(holdingItemFreeHands), "SPlayer somehow freed a hand after unforced drop of unremovable item.");
        
        Assert.That(HandSys.TryDrop(SPlayer, force:true), Is.True, "SPlayer was unable to drop the item despite being forced to drop it");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(initialFreeHands), "SPlayer did not return to initial free hand count after forcibly dropping the item!");
    }

    
}