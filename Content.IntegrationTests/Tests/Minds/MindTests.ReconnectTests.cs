using System.Linq;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

public sealed partial class MindTests
{
    // This test will do the following:
    // - attach a player to a ghost (not visiting)
    // - disconnect
    // - reconnect
    // - assert that they spawned in as a new entity
    [Test]
    public async Task TestGhostsCanReconnect()
    {
        await using var pairTracker = await SetupPair();
        var pair = pairTracker.Pair;
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mind = GetMind(pair);

        var ghost = await BecomeGhost(pair);
        await DisconnectReconnect(pair);

        // Player in control of a new ghost, but with the same mind
        Assert.Multiple(() =>
        {
            Assert.That(GetMind(pair), Is.EqualTo(mind));
            Assert.That(entMan.Deleted(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(mind.OwnedEntity));
            Assert.That(mind.VisitingEntity, Is.Null);
        });

        await pairTracker.CleanReturnAsync();
    }

    // This test will do the following:
    // - disconnect a player
    // - delete their original entity
    // - reconnect
    // - assert that they spawned in as a new entity
    [Test]
    public async Task TestDeletedCanReconnect()
    {
        await using var pairTracker = await SetupPair();
        var pair = pairTracker.Pair;
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mind = GetMind(pair);

        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.ServerSessions.Single();
        var name = player.Name;
        var user = player.UserId;
        Assert.That(mind.OwnedEntity, Is.Not.Null);
        var entity = mind.OwnedEntity.Value;

        // Player is not a ghost
        Assert.That(!entMan.HasComponent<GhostComponent>(mind.CurrentEntity));

        // Disconnect
        await Disconnect(pair);

        // Delete entity
        Assert.That(entMan.EntityExists(entity));
        await pair.Server.WaitPost(() => entMan.DeleteEntity(entity));
        Assert.Multiple(() =>
        {
            Assert.That(entMan.Deleted(entity));
            Assert.That(mind.OwnedEntity, Is.Null);
        });

        // Reconnect
        await Connect(pair, name);
        player = playerMan.ServerSessions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(user, Is.EqualTo(player.UserId));

            // Player is now a new ghost entity
            Assert.That(GetMind(pair), Is.EqualTo(mind));
            Assert.That(mind.OwnedEntity, Is.Not.EqualTo(entity));
            Assert.That(entMan.HasComponent<GhostComponent>(mind.OwnedEntity));
        });

        await pairTracker.CleanReturnAsync();
    }

    // This test will do the following:
    // - visit a ghost
    // - disconnect
    // - reconnect
    // - assert that they return to their original entity
    [Test]
    public async Task TestVisitingGhostReconnect()
    {
        await using var pairTracker = await SetupPair();
        var pair = pairTracker.Pair;
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mind = GetMind(pair);

        var original = mind.CurrentEntity;
        var ghost = await VisitGhost(pair);
        await DisconnectReconnect(pair);

        // Player now controls their original mob, mind was preserved
        Assert.Multiple(() =>
        {
            Assert.That(mind, Is.EqualTo(GetMind(pair)));
            Assert.That(mind.CurrentEntity, Is.EqualTo(original));
            Assert.That(entMan.Deleted(original), Is.False);
            Assert.That(entMan.Deleted(ghost));
        });

        await pairTracker.CleanReturnAsync();
    }

    // This test will do the following:
    // - visit a normal (non-ghost) entity,
    // - disconnect
    // - reconnect
    // - assert that they return to the visited entity.
    [Test]
    public async Task TestVisitingReconnect()
    {
        await using var pairTracker = await SetupPair();
        var pair = pairTracker.Pair;
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mindSys = entMan.System<MindSystem>();
        var mind = GetMind(pair);

        // Make player visit a new mob
        var original = mind.CurrentEntity;
        EntityUid visiting = default;
        await pair.Server.WaitAssertion(() =>
        {
            visiting = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            mindSys.Visit(mind, visiting);
        });
        await PoolManager.RunTicksSync(pair, 5);

        await DisconnectReconnect(pair);

        // Player is back in control of the visited mob, mind was preserved
        Assert.Multiple(() =>
        {
            Assert.That(GetMind(pair), Is.EqualTo(mind));
            Assert.That(entMan.Deleted(original), Is.False);
            Assert.That(entMan.Deleted(visiting), Is.False);
            Assert.That(mind.CurrentEntity, Is.EqualTo(visiting));
        });

        await pairTracker.CleanReturnAsync();
    }
}
