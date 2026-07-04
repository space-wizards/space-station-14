using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Stack;

namespace Content.IntegrationTests.Tests.Stacks;

[TestFixture]
[TestOf(typeof(StackSystem))]
public sealed class StackInteractionTest : InteractionTest
{
    [SidedDependency(Side.Server)] private readonly StackSystem _sStackSystem = default!;

    /// <summary>
    /// Test that using a stack on a stack will combine them to the hand.
    /// </summary>
    [Test]
    public async Task InteractUsingTest()
    {
        var spawnCount = StackTestPrototypes.Count1 + StackTestPrototypes.Count1;

        var held = await Spawn(StackTestPrototypes.StackEnt1);
        await Pickup(held);

        await SpawnTarget(StackTestPrototypes.StackEnt1);
        await Interact();

        Assert.Multiple(() =>
        {
            // Assert that the held stack has the full count
            // And the ground stack was deleted
            Assert.That(_sStackSystem.GetCount(ToServer(held)), Is.EqualTo(spawnCount));
            Assert.That(SEntMan.EntityExists(ToServer(Target)), Is.False);
        });
    }

    [Test]
    public async Task SplitTest()
    {
        await SpawnTarget(StackTestPrototypes.StackEnt30);
        await Interact(altInteract: true);

        Assert.That(_sStackSystem.GetCount(ToServer(Target).Value), Is.EqualTo(15));

        // TODO test split verb
        // I don't know how to navigate the right click menu in integration tests to find verbs
    }

    // TODO a test for eating a stack
    // Currently the player supplied by InteractionTest doesn't have a body or a stomach to eat with
    // And BodySystem has no API for cleanly adding the needed parts
}
