using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;

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
        var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = dirty
        });

        var entMan = pair.Server.ResolveDependency<IServerEntityManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var mindSys = entMan.System<SharedMindSystem>();

        var player = playerMan.Sessions.Single();

        EntityUid entity = default;
        EntityUid mindId = default!;
        MindComponent mind = default!;
        await pair.Server.WaitPost(() =>
        {
            entity = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            mindId = mindSys.CreateMind(player.UserId);
            mind = entMan.GetComponent<MindComponent>(mindId);
            mindSys.TransferTo(mindId, entity);
        });

        await pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(player.ContentData()?.Mind, Is.EqualTo(mindId));
            Assert.That(player.AttachedEntity, Is.EqualTo(entity));
            Assert.That(player.AttachedEntity, Is.EqualTo(mind.CurrentEntity), "Player is not attached to the mind's current entity.");
            Assert.That(entMan.EntityExists(mind.OwnedEntity), "The mind's current entity does not exist");
            Assert.That(mind.VisitingEntity == null || entMan.EntityExists(mind.VisitingEntity), "The minds visited entity does not exist.");
        });
        return pair;
    }

    private static async Task<EntityUid> BecomeGhost(TestPair pair, bool visit = false)
    {
        var entMan = pair.Server.ResolveDependency<IServerEntityManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var mindSys = entMan.System<SharedMindSystem>();
        EntityUid ghostUid = default;
        EntityUid mindId = default!;
        MindComponent mind = default!;

        var player = playerMan.Sessions.Single();
        await pair.Server.WaitAssertion(() =>
        {
            var oldUid = player.AttachedEntity;
            ghostUid = entMan.SpawnEntity(GameTicker.ObserverPrototypeName, MapCoordinates.Nullspace);
            mindId = mindSys.GetMind(player.UserId)!.Value;
            Assert.That(mindId, Is.Not.EqualTo(default(EntityUid)));
            mind = entMan.GetComponent<MindComponent>(mindId);

            if (visit)
            {
                mindSys.Visit(mindId, ghostUid);
                return;
            }

            mindSys.TransferTo(mindId, ghostUid);
            if (oldUid != null)
                entMan.DeleteEntity(oldUid.Value);

        });

        await pair.RunTicksSync(5);
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
    private static (EntityUid Id, MindComponent Comp) GetMind(Pair.TestPair pair)
    {
        var playerMan = pair.Server.PlayerMan;
        var entMan = pair.Server.EntMan;
        var player = playerMan.Sessions.SingleOrDefault();
        Assert.That(player, Is.Not.Null);

        var mindId = player.ContentData()!.Mind!.Value;
        Assert.That(mindId, Is.Not.EqualTo(default(EntityUid)));
        var mind = entMan.GetComponent<MindComponent>(mindId);
        ActorComponent actor = default!;
        Assert.Multiple(() =>
        {
            Assert.That(player, Is.EqualTo(mind.Session), "Player session does not match mind session");
            Assert.That(entMan.System<MindSystem>().GetMind(player.UserId), Is.EqualTo(mindId));
            Assert.That(player.AttachedEntity, Is.EqualTo(mind.CurrentEntity), "Player is not attached to the mind's current entity.");
            Assert.That(entMan.EntityExists(mind.OwnedEntity), "The mind's current entity does not exist");
            Assert.That(mind.VisitingEntity == null || entMan.EntityExists(mind.VisitingEntity), "The minds visited entity does not exist.");
            Assert.That(entMan.TryGetComponent(mind.CurrentEntity, out actor));
        });
        Assert.That(actor.PlayerSession, Is.EqualTo(mind.Session));

        return (mindId, mind);
    }

    private static async Task Disconnect(Pair.TestPair pair)
    {
        var netManager = pair.Client.ResolveDependency<IClientNetManager>();
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var player = playerMan.Sessions.Single();
        var mindId = player.ContentData()!.Mind!.Value;
        var mind = entMan.GetComponent<MindComponent>(mindId);

        await pair.Client.WaitAssertion(() =>
        {
            netManager.ClientDisconnect("Disconnect command used.");
        });
        await pair.RunTicksSync(5);

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
        Assert.That(!playerMan.Sessions.Any());

        await Task.WhenAll(pair.Client.WaitIdleAsync(), pair.Client.WaitIdleAsync());
        pair.Client.SetConnectTarget(pair.Server);
        await pair.Client.WaitPost(() => netManager.ClientConnect(null!, 0, username));
        await pair.RunTicksSync(5);

        var player = playerMan.Sessions.Single();
        Assert.That(player.Status, Is.EqualTo(SessionStatus.InGame));
    }

    private static async Task<ICommonSession> DisconnectReconnect(Pair.TestPair pair)
    {
        var playerMan = pair.Server.ResolveDependency<IPlayerManager>();
        var player = playerMan.Sessions.Single();
        var name = player.Name;
        var id = player.UserId;

        await Disconnect(pair);
        await Connect(pair, name);

        // Session has changed
        var newSession = playerMan.Sessions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(newSession, Is.Not.EqualTo(player));
            Assert.That(newSession.UserId, Is.EqualTo(id));
        });

        return newSession;
    }
}
