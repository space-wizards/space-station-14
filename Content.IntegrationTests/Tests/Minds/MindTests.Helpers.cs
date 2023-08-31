using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Players;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using IPlayerManager = Robust.Server.Player.IPlayerManager;

namespace Content.IntegrationTests.Tests.Minds;

// This partial class contains misc helper functions for other tests.
public sealed partial class MindTests
{
    /// <summary>
    /// Gets a server-client pair and ensures that the client is attached to a simple mind test entity.
    /// </summary>
    /// <remarks>
    /// Without this, it may be possible that a tests starts with the client attached to an entity that does not match
    /// the player's mind's current entity, likely because some previous test directly changed the players attached
    /// entity.
    /// </remarks>
    private static async Task<Pair.TestPair> SetupPair(bool dirty = false)
    {
        var pairTracker = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = dirty
        });
        var pair = pairTracker.Pair;

        var entMan = pair.Server.ResolveDependency<IServerEntityManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var mindSys = entMan.System<MindSystem>();

        var player = playerMan.ServerSessions.Single();

        EntityUid entity = default;
        Mind mind = default!;
        await pair.Server.WaitPost(() =>
        {
            entity = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            mind = mindSys.CreateMind(player.UserId);
            mindSys.TransferTo(mind, entity);
        });

        await PoolManager.RunTicksSync(pair, 5);

        Assert.Multiple(() =>
        {
            Assert.That(player.ContentData()?.Mind, Is.EqualTo(mind));
            Assert.That(player.AttachedEntity, Is.EqualTo(entity));
            Assert.That(player.AttachedEntity, Is.EqualTo(mind.CurrentEntity), "Player is not attached to the mind's current entity.");
            Assert.That(entMan.EntityExists(mind.OwnedEntity), "The mind's current entity does not exist");
            Assert.That(mind.VisitingEntity == null || entMan.EntityExists(mind.VisitingEntity), "The minds visited entity does not exist.");
        });
        return pairTracker;
    }

    private static async Task<EntityUid> BecomeGhost(TestPair pair, bool visit = false)
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
            Assert.That(mind, Is.Not.Null);

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
        Assert.Multiple(() =>
        {
            Assert.That(entMan.HasComponent<GhostComponent>(ghostUid));
            Assert.That(player.AttachedEntity, Is.EqualTo(ghostUid));
            Assert.That(mind.CurrentEntity, Is.EqualTo(ghostUid));
        });

        if (!visit)
            Assert.That(mind.VisitingEntity, Is.Null);

        return ghostUid;
    }

    private static async Task<EntityUid> VisitGhost(Pair.TestPair pair, bool _ = false)
    {
        return await BecomeGhost(pair, visit: true);
    }

    /// <summary>
    /// Get the player's current mind and check that the entities exists.
    /// </summary>
    private static Mind GetMind(Pair.TestPair pair)
    {
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var player = playerMan.ServerSessions.SingleOrDefault();
        Assert.That(player, Is.Not.Null);

        var mind = player.ContentData()!.Mind;
        Assert.That(mind, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(player.AttachedEntity, Is.EqualTo(mind.CurrentEntity), "Player is not attached to the mind's current entity.");
            Assert.That(entMan.EntityExists(mind.OwnedEntity), "The mind's current entity does not exist");
            Assert.That(mind.VisitingEntity == null || entMan.EntityExists(mind.VisitingEntity), "The minds visited entity does not exist.");
        });

        return mind;
    }

    private static async Task Disconnect(Pair.TestPair pair)
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

        Assert.Multiple(() =>
        {
            Assert.That(player.Status, Is.EqualTo(SessionStatus.Disconnected));
            Assert.That(mind.UserId, Is.Not.Null);
            Assert.That(mind.Session, Is.Null);
        });
    }

    private static async Task Connect(Pair.TestPair pair, string username)
    {
        var netManager = pair.Client.ResolveDependency<IClientNetManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        Assert.That(!playerMan.ServerSessions.Any());

        await Task.WhenAll(pair.Client.WaitIdleAsync(), pair.Client.WaitIdleAsync());
        pair.Client.SetConnectTarget(pair.Server);
        await pair.Client.WaitPost(() => netManager.ClientConnect(null!, 0, username));
        await PoolManager.RunTicksSync(pair, 5);

        var player = playerMan.ServerSessions.Single();
        Assert.That(player.Status, Is.EqualTo(SessionStatus.InGame));
    }

    private static async Task<IPlayerSession> DisconnectReconnect(Pair.TestPair pair)
    {
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.ServerSessions.Single();
        var name = player.Name;
        var id = player.UserId;

        await Disconnect(pair);
        await Connect(pair, name);

        // Session has changed
        var newSession = playerMan.ServerSessions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(newSession, Is.Not.EqualTo(player));
            Assert.That(newSession.UserId, Is.EqualTo(id));
        });

        return newSession;
    }
}
