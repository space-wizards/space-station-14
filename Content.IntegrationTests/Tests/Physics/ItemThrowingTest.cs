using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.Tests.Physics;

public sealed class ItemThrowingTest : InteractionTest
{
    /// <summary>
    /// Check that an egg breaks when thrown at a wall.
    /// </summary>
    [Test]
    [TestOf(typeof(ThrownItemComponent))]
    [TestOf(typeof(DamageOnHighSpeedImpactComponent))]
    public async Task TestThrownEggBreaks()
    {
        // Setup entities
        var egg = await PlaceInHands("FoodEgg");
        await SpawnTarget("WallSolid");
        await RunTicks(5);
        AssertExists(egg);

        // Currently not a "thrown" item.
        AssertComp<ThrownItemComponent>(hasComp: false, egg);
        Assert.That(Comp<PhysicsComponent>(egg).BodyStatus, Is.Not.EqualTo(BodyStatus.InAir));

        // Throw it.
        await ThrowItem();
        await RunTicks(1);
        AssertExists(egg);
        AssertComp<ThrownItemComponent>(hasComp: true, egg);
        Assert.That(Comp<PhysicsComponent>(egg).BodyStatus, Is.EqualTo(BodyStatus.InAir));

        // Splat
        await RunTicks(30);
        AssertDeleted(egg);
    }

    /// <summary>
    /// Check that an egg thrown into space continues to be an egg.
    /// I.e., verify that the deletions that happen in the other two tests aren't coincidental.
    /// </summary>
    [Test]
    //[TestOf(typeof(Egg))]
    public async Task TestEggIsEgg()
    {
        // Setup entities
        var egg = await PlaceInHands("FoodEgg");
        await RunTicks(5);
        AssertExists(egg);

        // Currently not a "thrown" item.
        AssertComp<ThrownItemComponent>(hasComp: false, egg);
        Assert.That(Comp<PhysicsComponent>(egg).BodyStatus, Is.Not.EqualTo(BodyStatus.InAir));

        // Throw it
        await ThrowItem();
        await RunTicks(5);
        AssertExists(egg);
        AssertComp<ThrownItemComponent>(hasComp: true, egg);
        Assert.That(Comp<PhysicsComponent>(egg).BodyStatus, Is.EqualTo(BodyStatus.InAir));

        // Wait a while
        await RunTicks(60);

        // Egg is egg
        AssertExists(egg);
        AssertPrototype("FoodEgg", egg);
        AssertComp<ThrownItemComponent>(hasComp: false, egg);
        Assert.That(Comp<PhysicsComponent>(egg).BodyStatus, Is.Not.EqualTo(BodyStatus.InAir));
    }

    /// <summary>
    /// Check that a physics can handle deleting a thrown entity. As to why this exists, see
    /// https://github.com/space-wizards/RobustToolbox/pull/4746
    /// </summary>
    [Test]
    [TestOf(typeof(ThrownItemComponent))]
    [TestOf(typeof(PhysicsComponent))]
    public async Task TestDeleteThrownItem()
    {
        // Setup entities
        var pen = await PlaceInHands("Pen");
        var physics = Comp<PhysicsComponent>(pen);
        await RunTicks(5);
        AssertExists(pen);

        // Currently not a "thrown" item.
        AssertComp<ThrownItemComponent>(hasComp: false, pen);
        Assert.That(physics.BodyStatus, Is.Not.EqualTo(BodyStatus.InAir));

        // Throw it
        await ThrowItem();
        await RunTicks(5);
        AssertExists(pen);
        AssertComp<ThrownItemComponent>(hasComp: true, pen);
        Assert.That(physics.BodyStatus, Is.EqualTo(BodyStatus.InAir));
        Assert.That(physics.CanCollide);

        // Attempt to make it sleep mid-air. This happens automatically due to the sleep timer, but we just do it manually.
        await Server.WaitPost(() => Server.System<PhysicsSystem>().SetAwake((ToServer(pen), physics), false));

        // Then try and delete it
        await Delete(pen);
        await RunTicks(5);
        AssertDeleted(pen);
    }
}

