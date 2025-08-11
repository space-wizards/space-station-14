using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Projectiles;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Embedding;

public sealed class EmbedTest : InteractionTest
{
    /// <summary>
    /// Embeddable entity that will be thrown at the target.
    /// </summary>
    private const string EmbeddableProtoId = "SurvivalKnife";

    /// <summary>
    /// Target entity that the thrown item will embed into.
    /// </summary>
    private const string TargetProtoId = "AirlockGlass";

    /// <summary>
    /// Embeds an entity with a <see cref="EmbeddableProjectileComponent"/> into a target,
    /// then disconnects the client. Intended to reveal any clientside issues that might
    /// occur due to reparenting during cleanup.
    /// </summary>
    [Test]
    public async Task TestDisconnectWhileEmbedded()
    {
        // Spawn the target we're going to throw at
        await SpawnTarget(TargetProtoId);

        // Give the player the embeddable to throw
        var projectile = await PlaceInHands(EmbeddableProtoId);
        Assert.That(TryComp<EmbeddableProjectileComponent>(projectile, out var embedComp),
            $"{EmbeddableProtoId} does not have EmbeddableProjectileComponent");
        // Make sure the projectile isn't already embedded into anything
        Assert.That(embedComp.EmbeddedIntoUid, Is.Null,
            $"Projectile already embedded into {SEntMan.ToPrettyString(embedComp.EmbeddedIntoUid)}");

        // Have the player throw the embeddable at the target
        await ThrowItem();

        // Wait a moment for the item to hit and embed
        await RunSeconds(0.5f);

        // Make sure the projectile is embedded into the target
        Assert.That(embedComp.EmbeddedIntoUid, Is.EqualTo(ToServer(Target)),
            "Projectile not embedded into target");

        // Disconnect the client
        var cNetMgr = Client.ResolveDependency<IClientNetManager>();
        await Client.WaitPost(Client.EntMan.FlushEntities);
        await Pair.RunTicksSync(1);
    }

    /// <summary>
    /// Embeds an entity with a <see cref="EmbeddableProjectileComponent"/> into a target,
    /// then deletes the target and makes sure the embeddable is not deleted.
    /// </summary>
    [Test]
    public async Task TestEmbedDetach()
    {
        // Spawn the target we're going to throw at
        await SpawnTarget(TargetProtoId);

        // Give the player the embeddable to throw
        var projectile = await PlaceInHands(EmbeddableProtoId);
        Assert.That(TryComp<EmbeddableProjectileComponent>(projectile, out var embedComp),
            $"{EmbeddableProtoId} does not have EmbeddableProjectileComponent");
        // Make sure the projectile isn't already embedded into anything
        Assert.That(embedComp.EmbeddedIntoUid, Is.Null,
            $"Projectile already embedded into {SEntMan.ToPrettyString(embedComp.EmbeddedIntoUid)}");

        // Have the player throw the embeddable at the target
        await ThrowItem();

        // Wait a moment for the item to hit and embed
        await RunSeconds(0.5f);

        // Make sure the projectile is embedded into the target
        Assert.That(embedComp.EmbeddedIntoUid, Is.EqualTo(ToServer(Target)),
            "Projectile not embedded into target");

        // Delete the target
        await Delete(Target.Value);

        await RunTicks(1);

        // Make sure the embeddable wasn't deleted with the target
        AssertExists(projectile);
        await AssertEntityLookup(EmbeddableProtoId);
    }

    /// <summary>
    /// Throws two embeddable projectiles at a target, then deletes them
    /// one at a time, making sure that they are tracked correctly and that
    /// the <see cref="EmbeddedContainerComponent"/> is removed once all
    /// projectiles are gone.
    /// </summary>
    [Test]
    public async Task TestDeleteWhileEmbedded()
    {
        // Spawn the target we're going to throw at
        await SpawnTarget(TargetProtoId);

        // Give the player the embeddable to throw
        var projectile1 = await PlaceInHands(EmbeddableProtoId);
        Assert.That(TryComp<EmbeddableProjectileComponent>(projectile1, out var embedComp),
            $"{EmbeddableProtoId} does not have EmbeddableProjectileComponent.");
        // Make sure the projectile isn't already embedded into anything
        Assert.That(embedComp.EmbeddedIntoUid, Is.Null,
            $"Projectile already embedded into {SEntMan.ToPrettyString(embedComp.EmbeddedIntoUid)}.");

        // Have the player throw the embeddable at the target
        await ThrowItem();

        // Give the player a second embeddable to throw
        var projectile2 = await PlaceInHands(EmbeddableProtoId);
        Assert.That(TryComp<EmbeddableProjectileComponent>(projectile1, out var embedComp2),
            $"{EmbeddableProtoId} does not have EmbeddableProjectileComponent.");

        // Wait a moment for the projectile to hit and embed
        await RunSeconds(0.5f);

        // Make sure the projectile is embedded into the target
        Assert.That(embedComp.EmbeddedIntoUid, Is.EqualTo(ToServer(Target)),
            "First projectile not embedded into target.");
        Assert.That(TryComp<EmbeddedContainerComponent>(out var containerComp),
            "Target was not given EmbeddedContainerComponent.");
        Assert.That(containerComp.EmbeddedObjects, Does.Contain(ToServer(projectile1)),
            "Target is not tracking the first projectile as embedded.");
        Assert.That(containerComp.EmbeddedObjects, Has.Count.EqualTo(1),
            "Target has unexpected EmbeddedObjects count.");

        // Wait for the cooldown between throws
        await RunSeconds(Hands.ThrowCooldown.Seconds);

        // Throw the second projectile
        await ThrowItem();

        // Wait a moment for the second projectile to hit and embed
        await RunSeconds(0.5f);

        Assert.That(embedComp2.EmbeddedIntoUid, Is.EqualTo(ToServer(Target)),
            "Second projectile not embedded into target");
        AssertComp<EmbeddedContainerComponent>();
        Assert.That(containerComp.EmbeddedObjects, Does.Contain(ToServer(projectile1)),
            "Target is not tracking the second projectile as embedded.");
        Assert.That(containerComp.EmbeddedObjects, Has.Count.EqualTo(2),
            "Target EmbeddedObjects count did not increase with second projectile.");

        // Delete the first projectile
        await Delete(projectile1);

        Assert.That(containerComp.EmbeddedObjects, Does.Not.Contain(ToServer(projectile1)),
            "Target did not stop tracking first projectile after it was deleted.");
        Assert.That(containerComp.EmbeddedObjects, Does.Not.Contain(EntityUid.Invalid),
            "Target EmbeddedObjects contains an invalid entity.");
        foreach (var embedded in containerComp.EmbeddedObjects)
        {
            Assert.That(!SEntMan.Deleted(embedded),
                "Target EmbeddedObjects contains a deleted entity.");
        }
        Assert.That(containerComp.EmbeddedObjects, Has.Count.EqualTo(1),
            "Target EmbeddedObjects count did not decrease after deleting first projectile.");

        // Delete the second projectile
        await Delete(projectile2);

        Assert.That(!SEntMan.HasComponent<EmbeddedContainerComponent>(ToServer(Target)),
            "Target did not remove EmbeddedContainerComponent after both projectiles were deleted.");
    }
}
