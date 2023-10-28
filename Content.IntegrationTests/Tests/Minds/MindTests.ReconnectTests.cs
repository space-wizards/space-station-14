using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using NUnit.Framework.Interfaces;
using Robust.Server.GameObjects;
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
        await using var pair = await SetupPair();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mind = GetMind(pair);

        var ghost = await BecomeGhost(pair);
        await DisconnectReconnect(pair);

        // Player in control of a new ghost, but with the same mind
        Assert.Multiple(() =>
        {
            Assert.That(GetMind(pair), Is.EqualTo(mind));
            Assert.That(entMan.Deleted(ghost));
            Assert.That(entMan.HasComponent<GhostComponent>(mind.Comp.OwnedEntity));
            Assert.That(mind.Comp.VisitingEntity, Is.Null);
        });

        await pair.CleanReturnAsync();
    }

    // This test will do the following:
    // - disconnect a player
    // - delete their original entity
    // - reconnect
    // - assert that they spawned in as a new entity
    [Test]
    public async Task TestDeletedCanReconnect()
    {
        await using var pair = await SetupPair();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mind = GetMind(pair);

        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.Sessions.Single();
        var name = player.Name;
        var user = player.UserId;
        Assert.That(mind.Comp.OwnedEntity, Is.Not.Null);
        var entity = mind.Comp.OwnedEntity.Value;

        // Player is not a ghost
        Assert.That(!entMan.HasComponent<GhostComponent>(mind.Comp.CurrentEntity));

        // Disconnect
        await Disconnect(pair);

        // Delete entity
        Assert.That(entMan.EntityExists(entity));
        await pair.Server.WaitPost(() => entMan.DeleteEntity(entity));
        Assert.Multiple(() =>
        {
            Assert.That(entMan.Deleted(entity));
            Assert.That(mind.Comp.OwnedEntity, Is.Null);
        });

        // Reconnect
        await Connect(pair, name);
        player = playerMan.Sessions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(user, Is.EqualTo(player.UserId));

            // Player is now a new ghost entity
            Assert.That(GetMind(pair), Is.EqualTo(mind));
            Assert.That(mind.Comp.OwnedEntity, Is.Not.EqualTo(entity));
            Assert.That(entMan.HasComponent<GhostComponent>(mind.Comp.OwnedEntity));
        });

        await pair.CleanReturnAsync();
    }

    // This test will do the following:
    // - visit a ghost
    // - disconnect
    // - reconnect
    // - assert that they return to their original entity
    [Test]
    public async Task TestVisitingGhostReconnect()
    {
        await using var pair = await SetupPair();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mind = GetMind(pair);

        var original = mind.Comp.CurrentEntity;
        var ghost = await VisitGhost(pair);
        await DisconnectReconnect(pair);

        // Player now controls their original mob, mind was preserved
        Assert.Multiple(() =>
        {
            Assert.That(mind, Is.EqualTo(GetMind(pair)));
            Assert.That(mind.Comp.CurrentEntity, Is.EqualTo(original));
            Assert.That(entMan.Deleted(original), Is.False);
            Assert.That(entMan.Deleted(ghost));
        });

        await pair.CleanReturnAsync();
    }

    // This test will do the following:
    // - visit a normal (non-ghost) entity,
    // - disconnect
    // - reconnect
    // - assert that they return to the visited entity.
    [Test]
    public async Task TestVisitingReconnect()
    {
        await using var pair = await SetupPair();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mindSys = entMan.System<SharedMindSystem>();
        var mind = GetMind(pair);

        Assert.Null(mind.Comp.VisitingEntity);

        // Make player visit a new mob
        var original = mind.Comp.OwnedEntity;
        EntityUid visiting = default;
        await pair.Server.WaitAssertion(() =>
        {
            visiting = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            mindSys.Visit(mind.Id, visiting);
        });
        await pair.RunTicksSync(5);

        Assert.That(mind.Comp.VisitingEntity, Is.EqualTo(visiting));
        await DisconnectReconnect(pair);

        // Player is back in control of the visited mob, mind was preserved
        Assert.Multiple(() =>
        {
            Assert.That(GetMind(pair), Is.EqualTo(mind));
            Assert.That(entMan.Deleted(original), Is.False);
            Assert.That(entMan.Deleted(visiting), Is.False);
            Assert.That(mind.Comp.CurrentEntity, Is.EqualTo(visiting));
        });

        await pair.CleanReturnAsync();
    }

    // This test will do the following
    // - connect as a normal player
    // - disconnect
    // - reconnect
    // - assert that they return to the original entity.
    [Test]
    public async Task TestReconnect()
    {
        await using var pair = await SetupPair();
        var mind = GetMind(pair);

        Assert.Null(mind.Comp.VisitingEntity);
        Assert.NotNull(mind.Comp.OwnedEntity);
        var entity = mind.Comp.OwnedEntity;

        await pair.RunTicksSync(5);
        await DisconnectReconnect(pair);
        await pair.RunTicksSync(5);

        var newMind = GetMind(pair);

        Assert.Null(newMind.Comp.VisitingEntity);
        Assert.That(newMind.Comp.OwnedEntity, Is.EqualTo(entity));
        Assert.That(newMind.Id, Is.EqualTo(mind.Id));

        await pair.CleanReturnAsync();
    }
}
