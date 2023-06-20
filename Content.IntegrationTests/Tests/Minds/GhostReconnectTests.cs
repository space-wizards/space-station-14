using System.Linq;
using System.Threading.Tasks;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Players;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class GhostReconnectTests
{
    private const string Prototypes = @"
- type: entity
  id: GhostReconnectTestEntity
  components:
  - type: MindContainer
";

    // This test will do the following:
    // - attach a player to a ghost (not visiting)
    // - disconnect
    // - reconnect
    // - spawned in as a new entity
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
        var ghost = await BecomeGhost(pair, visit: true);
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
            visiting = entMan.SpawnEntity("GhostReconnectTestEntity", MapCoordinates.Nullspace);
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

    public async Task<EntityUid> BecomeGhost(Pair pair, bool visit = false)
    {
        var entMan = pair.Server.ResolveDependency<IServerEntityManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var mindSys = entMan.System<MindSystem>();
        EntityUid ghostUid = default;
        Mind mind = default!;

        var player = playerMan.ServerSessions.Single();
        await pair.Server.WaitAssertion(() =>
        {
            var oldUid = player.AttachedEntity;
            ghostUid = entMan.SpawnEntity("MobObserver", MapCoordinates.Nullspace);
            mind = mindSys.GetMind(player.UserId);
            Assert.NotNull(mind);

            if (visit)
            {
                mindSys.Visit(mind, ghostUid);
                return;
            }

            mindSys.TransferTo(mind, ghostUid);
            if (oldUid != null)
                entMan.DeleteEntity(oldUid.Value);

        });

        await PoolManager.RunTicksSync(pair, 5);
        Assert.That(entMan.HasComponent<GhostComponent>(ghostUid));
        Assert.That(player.AttachedEntity == ghostUid);
        Assert.That(mind.CurrentEntity == ghostUid);

        if (!visit)
            Assert.Null(mind.VisitingEntity);

        return ghostUid;
    }

    /// <summary>
    /// Check that the player exists and the mind has been properly set up.
    /// </summary>
    public Mind GetMind(Pair pair)
    {
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();

        var player = playerMan.ServerSessions.SingleOrDefault();
        Assert.NotNull(player);

        var mind = player.ContentData()!.Mind;
        Assert.NotNull(mind);
        Assert.That(player.AttachedEntity == mind.CurrentEntity);
        Assert.That(entMan.EntityExists(mind.OwnedEntity));
        Assert.That(entMan.EntityExists(mind.CurrentEntity));

        return mind;
    }

    public async Task Disconnect(Pair pair)
    {
        var netManager = pair.Client.ResolveDependency<IClientNetManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.ServerSessions.Single();
        var mind = player.ContentData()!.Mind;

        await pair.Client.WaitAssertion(() =>
        {
            netManager.ClientDisconnect("Disconnect command used.");
        });
        await PoolManager.RunTicksSync(pair, 5);

        Assert.That(player.Status == SessionStatus.Disconnected);
        Assert.NotNull(mind.UserId);
        Assert.Null(mind.Session);
    }

    public async Task Connect(Pair pair, string username)
    {
        var netManager = pair.Client.ResolveDependency<IClientNetManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        Assert.That(!playerMan.ServerSessions.Any());

        await Task.WhenAll(pair.Client.WaitIdleAsync(), pair.Client.WaitIdleAsync());
        pair.Client.SetConnectTarget(pair.Server);
        await pair.Client.WaitPost(() => netManager.ClientConnect(null!, 0, username));
        await PoolManager.RunTicksSync(pair, 5);

        var player = playerMan.ServerSessions.Single();
        Assert.That(player.Status == SessionStatus.InGame);
    }

    public async Task<IPlayerSession> DisconnectReconnect(Pair pair)
    {
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.ServerSessions.Single();
        var mind = player.ContentData()!.Mind;
        var name = player.Name;
        var id = player.UserId;

        await Disconnect(pair);
        await Connect(pair, name);

        // Session has changed
        var newSession = playerMan.ServerSessions.Single();
        Assert.That(newSession != player);
        Assert.That(newSession.UserId == id);

        return newSession;
    }
}
