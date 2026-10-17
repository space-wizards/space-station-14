using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Stack;
using static Content.IntegrationTests.Tests.Stacks.StackTestPrototypes;

namespace Content.IntegrationTests.Tests.Stacks;

[TestFixture]
[TestOf(typeof(StackSystem))]
public sealed class StackInteractionTest : InteractionTest
{
    [SidedDependency(Side.Server)] private readonly StackSystem _sStackSystem = default!;

    [Test]
    [Description("Test that using a stack on a stack will combine them to the hand.")]
    public async Task InteractUsingTest()
    {
        var held = await Spawn(StackEnt1);
        await Pickup(held);

        await SpawnTarget(StackEnt1);
        await Interact();

        using (Assert.EnterMultipleScope())
        {
            // Assert that the held stack has the full count
            // And the ground stack was deleted
            Assert.That(_sStackSystem.GetCount(ToServer(held)), Is.EqualTo(2));
            Assert.That(SEntMan.EntityExists(ToServer(Target)), Is.False);
        }
    }

    [Test]
    [Description("Test alt interact with a stack splits it in half.")]
    public async Task SplitTest()
    {
        await SpawnTarget(StackEnt30);
        await Interact(altInteract: true);

        var targetCount = 0;
        var heldCount = 0;
        await Server.WaitPost(() =>
        {
             targetCount = _sStackSystem.GetCount(STarget.Value);

            if (HandSys.GetActiveItem((ToServer(Player), Hands)) is { } held)
                    heldCount = _sStackSystem.GetCount(held);
        });

        // Check that the count was evenly split
        using (Assert.EnterMultipleScope())
        {
            Assert.That(targetCount, Is.EqualTo(15));
            Assert.That(heldCount, Is.EqualTo(15));
        }
    }

    // TODO test split verb
    // I don't know how to navigate the right click menu in integration tests to find verbs

    // TODO a test for eating a stack
    // Currently the player supplied by InteractionTest doesn't have a body or a stomach to eat with
    // And BodySystem has no API for cleanly adding the needed parts
}
