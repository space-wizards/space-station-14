using System.Linq;
using System.Threading.Tasks;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
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
        await using var pairTracker = await PoolManager.GetServerClient();
        var pair = pairTracker.Pair;

        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        await PoolManager.RunTicksSync(pair, 5);
        var mind = GetMind(pair);

        var ghost = await BecomeGhost(pair);
        await DisconnectReconnect(pair);

        // Player in control of a NEW entity
        var newMind = GetMind(pair);
        Assert.That(newMind != mind);
        Assert.That(entMan.Deleted(ghost));
        Assert.Null(newMind.VisitingEntity);

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
        await using var pairTracker = await PoolManager.GetServerClient();
        var pair = pairTracker.Pair;

        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        await PoolManager.RunTicksSync(pair, 5);
        var mind = GetMind(pair);

        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.ServerSessions.Single();
        var name = player.Name;
        var user = player.UserId;
        Assert.NotNull(mind.OwnedEntity);
        var entity = mind.OwnedEntity.Value;

        // Player is not a ghost
        Assert.That(!entMan.HasComponent<GhostComponent>(mind.CurrentEntity));

        // Disconnect
        await Disconnect(pair);

        // Delete entity
        Assert.That(entMan.EntityExists(entity));
        await pair.Server.WaitPost(() => entMan.DeleteEntity(entity));
        Assert.That(entMan.Deleted(entity));
        Assert.IsNull(mind.OwnedEntity);

        // Reconnect
        await Connect(pair, name);
        player = playerMan.ServerSessions.Single();
        Assert.That(user == player.UserId);

        // Player is now a new entity
        var newMind = GetMind(pair);
        Assert.That(newMind != mind);
        Assert.Null(mind.UserId);
        Assert.Null(mind.CurrentEntity);
        Assert.NotNull(newMind.OwnedEntity);
        Assert.That(entMan.EntityExists(newMind.OwnedEntity));

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
        await using var pairTracker = await PoolManager.GetServerClient();
        var pair = pairTracker.Pair;

        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        await PoolManager.RunTicksSync(pair, 5);
        var mind = GetMind(pair);

        var original = mind.CurrentEntity;
        var ghost = await VisitGhost(pair);
        await DisconnectReconnect(pair);

        // Player now controls their original mob, mind was preserved
        Assert.That(mind == GetMind(pair));
        Assert.That(mind.CurrentEntity == original);
        Assert.That(!entMan.Deleted(original));
        Assert.That(entMan.Deleted(ghost));

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
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ ExtraPrototypes = Prototypes });
        var pair = pairTracker.Pair;

        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mindSys = entMan.System<MindSystem>();
        await PoolManager.RunTicksSync(pair, 5);
        var mind = GetMind(pair);

        // Make player visit a new mob
        var original = mind.CurrentEntity;
        EntityUid visiting = default;
        await pair.Server.WaitAssertion(() =>
        {
            visiting = entMan.SpawnEntity("MindTestEntity", MapCoordinates.Nullspace);
            mindSys.Visit(mind, visiting);
        });
        await PoolManager.RunTicksSync(pair, 5);

        await DisconnectReconnect(pair);

        // Player is back in control of the visited mob, mind was preserved
        Assert.That(mind == GetMind(pair));
        Assert.That(!entMan.Deleted(original));
        Assert.That(!entMan.Deleted(visiting));
        Assert.That(mind.CurrentEntity == visiting);
        Assert.That(mind.CurrentEntity == visiting);

        await pairTracker.CleanReturnAsync();
    }
}
