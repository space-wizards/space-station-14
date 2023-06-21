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
using IPlayerManager = Robust.Server.Player.IPlayerManager;

namespace Content.IntegrationTests.Tests.Minds;

// This partial class contains misc helper functions for other tests.
[TestFixture]
public sealed partial class MindTests
{
    /// <summary>
    ///     Gets a server-client pair and ensures that the client is attached to a simple mind test entity.
    /// </summary>
    public async Task<PairTracker> SetupPair()
    {
        var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ ExtraPrototypes = Prototypes });
        var pair = pairTracker.Pair;

        var entMan = pair.Server.ResolveDependency<IServerEntityManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var mindSys = entMan.System<MindSystem>();

        var player = playerMan.ServerSessions.Single();

        EntityUid entity = default;
        await pair.Server.WaitPost(() =>
        {
            entity = entMan.SpawnEntity("MindTestEntity", MapCoordinates.Nullspace);
            mindSys.TransferTo(mindSys.CreateMind(player.UserId), entity);
        });

        await PoolManager.RunTicksSync(pair, 5);

        var mind = player.ContentData()?.Mind;
        Assert.NotNull(mind);
        Assert.That(player.AttachedEntity, Is.EqualTo(entity));
        Assert.That(player.AttachedEntity, Is.EqualTo(mind.CurrentEntity), "Player is not attached to the mind's current entity.");
        Assert.That(entMan.EntityExists(mind.OwnedEntity), "The mind's current entity does not exist");
        Assert.That(mind.VisitingEntity == null || entMan.EntityExists(mind.VisitingEntity), "The minds visited entity does not exist.");
        return pairTracker;
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

    public async Task<EntityUid> VisitGhost(Pair pair, bool visit = false)
    {
        return await BecomeGhost(pair, visit: true);
    }

    /// <summary>
    /// Get the player's current mind and check that the entities exists.
    /// </summary>
    public Mind GetMind(Pair pair)
    {
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var player = playerMan.ServerSessions.SingleOrDefault();
        Assert.NotNull(player);

        var mind = player.ContentData()!.Mind;
        Assert.NotNull(mind);
        Assert.That(player.AttachedEntity, Is.EqualTo(mind.CurrentEntity), "Player is not attached to the mind's current entity.");
        Assert.That(entMan.EntityExists(mind.OwnedEntity), "The mind's current entity does not exist");
        Assert.That(mind.VisitingEntity == null || entMan.EntityExists(mind.VisitingEntity), "The minds visited entity does not exist.");

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
