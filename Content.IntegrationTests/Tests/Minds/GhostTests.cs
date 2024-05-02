using System.Numerics;
using Content.IntegrationTests.Pair;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class GhostTests
{
    struct GhostTestData
    {
        public IEntityManager SEntMan;
        public Robust.Server.Player.IPlayerManager SPlayerMan;
        public Server.Mind.MindSystem SMindSys;
        public SharedTransformSystem STransformSys = default!;

        public TestPair Pair = default!;

        public TestMapData MapData => Pair.TestMap!;

        public RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;
        public RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;

        /// <summary>
        /// Initial player coordinates. Note that this does not necessarily correspond to the position of the
        /// <see cref="Player"/> entity.
        /// </summary>
        public NetCoordinates PlayerCoords = default!;

        public NetEntity Player = default!;
        public EntityUid SPlayerEnt = default!;

        public ICommonSession ClientSession = default!;
        public ICommonSession ServerSession = default!;

        public GhostTestData()
        {
        }
    }

    private async Task<GhostTestData> SetupData()
    {
        var data = new GhostTestData();

        // Client is needed to create a session for the ghost system. Creating a dummy session was too difficult.
        data.Pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        });

        data.SEntMan = data.Pair.Server.ResolveDependency<IServerEntityManager>();
        data.SPlayerMan = data.Pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        data.SMindSys = data.SEntMan.System<Server.Mind.MindSystem>();
        data.STransformSys = data.SEntMan.System<SharedTransformSystem>();

        // Setup map.
        await data.Pair.CreateTestMap();
        data.PlayerCoords = data.SEntMan.GetNetCoordinates(data.MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)).WithEntityId(data.MapData.MapUid, data.STransformSys, data.SEntMan));

        if (data.Client.Session == null)
            Assert.Fail("No player");
        data.ClientSession = data.Client.Session!;
        data.ServerSession = data.SPlayerMan.GetSessionById(data.ClientSession.UserId);

        Entity<MindComponent> mind = default!;
        await data.Pair.Server.WaitPost(() =>
        {
            data.Player = data.SEntMan.GetNetEntity(data.SEntMan.SpawnEntity(null, data.SEntMan.GetCoordinates(data.PlayerCoords)));
            mind = data.SMindSys.CreateMind(data.ServerSession.UserId, "DummyPlayerEntity");
            data.SPlayerEnt = data.SEntMan.GetEntity(data.Player);
            data.SMindSys.TransferTo(mind, data.SPlayerEnt, mind: mind.Comp);
            data.Server.PlayerMan.SetAttachedEntity(data.ServerSession, data.SPlayerEnt);
        });

        await data.Pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(data.ServerSession.ContentData()?.Mind, Is.EqualTo(mind.Owner));
            Assert.That(data.ServerSession.AttachedEntity, Is.EqualTo(data.SPlayerEnt));
            Assert.That(data.ServerSession.AttachedEntity, Is.EqualTo(mind.Comp.CurrentEntity),
                "Player is not attached to the mind's current entity.");
            Assert.That(data.SEntMan.EntityExists(mind.Comp.OwnedEntity),
                "The mind's current entity does not exist");
            Assert.That(mind.Comp.VisitingEntity == null || data.SEntMan.EntityExists(mind.Comp.VisitingEntity),
                "The minds visited entity does not exist.");
        });

        Assert.That(data.SPlayerEnt, Is.Not.EqualTo(null));

        return data;
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnDelete()
    {
        var data = await SetupData();

        var oldPosition = data.SEntMan.GetComponent<TransformComponent>(data.SPlayerEnt).Coordinates;

        Assert.That(!data.SEntMan.HasComponent<GhostComponent>(data.SPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await data.Server.WaitPost(() => data.SEntMan.DeleteEntity(data.SPlayerEnt));
        await data.Pair.RunTicksSync(5);

        var ghost = data.ServerSession.AttachedEntity!.Value;
        Assert.That(data.SEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = data.SEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await data.Pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is queue deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnQueueDelete()
    {
        var data = await SetupData();

        var oldPosition = data.SEntMan.GetComponent<TransformComponent>(data.SPlayerEnt).Coordinates;

        Assert.That(!data.SEntMan.HasComponent<GhostComponent>(data.SPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await data.Server.WaitPost(() => data.SEntMan.QueueDeleteEntity(data.SPlayerEnt));
        await data.Pair.RunTicksSync(5);

        var ghost = data.ServerSession.AttachedEntity!.Value;
        Assert.That(data.SEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = data.SEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await data.Pair.CleanReturnAsync();
    }

}
