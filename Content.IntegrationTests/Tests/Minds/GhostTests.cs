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
        public TestPair _pair = default!;

        public TestMapData MapData => _pair.TestMap!;

        public RobustIntegrationTest.ServerIntegrationInstance Server => _pair.Server;
        public RobustIntegrationTest.ClientIntegrationInstance Client => _pair.Client;

        /// <summary>
        /// Initial player coordinates. Note that this does not necessarily correspond to the position of the
        /// <see cref="_player"/> entity.
        /// </summary>
        public NetCoordinates _playerCoords = default!;

        public NetEntity _player = default!;
        public EntityUid _sPlayerEnt = default!;

        public ICommonSession _clientSession = default!;
        public ICommonSession _serverSession = default!;

        public IEntityManager _sEntMan;
        public Robust.Server.Player.IPlayerManager _sPlayerMan;
        public Server.Mind.MindSystem _sMindSys;
        public SharedTransformSystem _sTransformSys = default!;

        public GhostTestData()
        {
        }
    }

    // [SetUp]
    private async Task<GhostTestData> Setup()
    {
        var testData = new GhostTestData();
        // Client is needed to create a session for the ghost system. Creating a dummy session was too difficult.
        testData._pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        });

        testData._sEntMan = testData._pair.Server.ResolveDependency<IServerEntityManager>();
        testData._sPlayerMan = testData._pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        testData._sMindSys = testData._sEntMan.System<Server.Mind.MindSystem>();
        testData._sTransformSys = testData._sEntMan.System<SharedTransformSystem>();

        // Setup map.
        await testData._pair.CreateTestMap();
        testData._playerCoords = testData._sEntMan.GetNetCoordinates(testData.MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)).WithEntityId(testData.MapData.MapUid, testData._sTransformSys, testData._sEntMan));

        if (testData.Client.Session == null)
            Assert.Fail("No player");
        testData._clientSession = testData.Client.Session!;
        testData._serverSession = testData._sPlayerMan.GetSessionById(testData._clientSession.UserId);

        Entity<MindComponent> mind = default!;
        await testData._pair.Server.WaitPost(() =>
        {
            testData._player = testData._sEntMan.GetNetEntity(testData._sEntMan.SpawnEntity(null, testData._sEntMan.GetCoordinates(testData._playerCoords)));
            mind = testData._sMindSys.CreateMind(testData._serverSession.UserId, "DummyPlayerEntity");
            testData._sPlayerEnt = testData._sEntMan.GetEntity(testData._player);
            testData._sMindSys.TransferTo(mind, testData._sPlayerEnt, mind: mind.Comp);
            testData.Server.PlayerMan.SetAttachedEntity(testData._serverSession, testData._sPlayerEnt);
        });

        await testData._pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(testData._serverSession.ContentData()?.Mind, Is.EqualTo(mind.Owner));
            Assert.That(testData._serverSession.AttachedEntity, Is.EqualTo(testData._sPlayerEnt));
            Assert.That(testData._serverSession.AttachedEntity, Is.EqualTo(mind.Comp.CurrentEntity),
                "Player is not attached to the mind's current entity.");
            Assert.That(testData._sEntMan.EntityExists(mind.Comp.OwnedEntity),
                "The mind's current entity does not exist");
            Assert.That(mind.Comp.VisitingEntity == null || testData._sEntMan.EntityExists(mind.Comp.VisitingEntity),
                "The minds visited entity does not exist.");
        });
        return testData;
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnDelete()
    {
        var testData = await Setup();
        Assert.That(testData._sPlayerEnt, Is.Not.EqualTo(null));
        var oldPosition = testData._sEntMan.GetComponent<TransformComponent>(testData._sPlayerEnt).Coordinates;

        Assert.That(!testData._sEntMan.HasComponent<GhostComponent>(testData._sPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await testData.Server.WaitPost(() => testData._sEntMan.DeleteEntity(testData._sPlayerEnt));
        await testData._pair.RunTicksSync(5);

        var ghost = testData._serverSession.AttachedEntity!.Value;
        Assert.That(testData._sEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = testData._sEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await testData._pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is queue deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnQueueDelete()
    {
        var testData = await Setup();
        Assert.That(testData._sPlayerEnt, Is.Not.EqualTo(null));
        var oldPosition = testData._sEntMan.GetComponent<TransformComponent>(testData._sPlayerEnt).Coordinates;

        Assert.That(!testData._sEntMan.HasComponent<GhostComponent>(testData._sPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await testData.Server.WaitPost(() => testData._sEntMan.QueueDeleteEntity(testData._sPlayerEnt));
        await testData._pair.RunTicksSync(5);

        var ghost = testData._serverSession.AttachedEntity!.Value;
        Assert.That(testData._sEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = testData._sEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await testData._pair.CleanReturnAsync();
    }

}
