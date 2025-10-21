using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Hands;

public sealed class WieldingRequiresMultipleHandsTest : InteractionTest
{
    [TestPrototypes]
    private const string TestProto = @"
- type: entity
  id: TestWieldingRequiresMultipleHandsTest
  name: rayray
  components:
  - type: Item
    size: Large
  - type: Wieldable
";

    [Test]
    public async Task TestOneHandedWielding()
    {
        var itemNet = await PlaceInHands("TestWieldingRequiresMultipleHandsTest");
        var wieldComp = Comp<WieldableComponent>(itemNet);

        // The player entity by default should only have one hand.
        // We're checking to make sure that is still the case.
        var handCount = HandSys.GetHandCount(ToServer(Player));

        Assert.That(handCount,
            Is.EqualTo(1),
            "Player entity has more than one hand! If this is intentional, " +
            "WieldingRequiresMultipleHandsTest should be removed!");
        Assert.That(wieldComp.Wielded, Is.False, "Item spawned in wielded!");
        await UseInHand();
        Assert.That(wieldComp.Wielded, Is.False, "Item was wielded but player only has one hand!");
    }
}
