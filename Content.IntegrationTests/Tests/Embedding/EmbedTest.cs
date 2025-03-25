using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Projectiles;
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
}
