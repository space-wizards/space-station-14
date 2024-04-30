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
    private TestPair Pair = default!;

    protected TestMapData MapData => Pair.TestMap!;

    private RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;
    private RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;

    /// <summary>
    /// Initial player coordinates. Note that this does not necessarily correspond to the position of the
    /// <see cref="Player"/> entity.
    /// </summary>
    protected NetCoordinates PlayerCoords;

    private NetEntity Player;
    private EntityUid SPlayerEnt;

    private EntityUid SPlayer => ToServer(Player);

    protected ICommonSession ClientSession = default!;
    protected ICommonSession ServerSession = default!;

    private IEntityManager SEntMan;
    private Robust.Server.Player.IPlayerManager SPlayerMan;
    private Server.Mind.MindSystem SMindSys;
    private SharedTransformSystem STransformSys = default!;

    #region Networking

    protected EntityUid ToServer(NetEntity nent) => SEntMan.GetEntity(nent);

    #endregion

    [SetUp]
    public async Task Setup()
    {
        // Client is needed to create a session for the ghost system. Creating a dummy session was too difficult.
        Pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        });

        SEntMan = Pair.Server.ResolveDependency<IServerEntityManager>();
        SPlayerMan = Pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        SMindSys = SEntMan.System<Server.Mind.MindSystem>();
        STransformSys = SEntMan.System<SharedTransformSystem>();

        // Setup map.
        await Pair.CreateTestMap();
        PlayerCoords = SEntMan.GetNetCoordinates(MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)).WithEntityId(MapData.MapUid, STransformSys, SEntMan));

        if (Client.Session == null)
            Assert.Fail("No player");
        ClientSession = Client.Session!;
        ServerSession = SPlayerMan.GetSessionById(ClientSession.UserId);

        Entity<MindComponent> mind = default!;
        await Pair.Server.WaitPost(() =>
        {
            Player = SEntMan.GetNetEntity(SEntMan.SpawnEntity(null, SEntMan.GetCoordinates(PlayerCoords)));
            mind = SMindSys.CreateMind(ServerSession.UserId, "DummyPlayerEntity");
            SPlayerEnt = SEntMan.GetEntity(Player);
            SMindSys.TransferTo(mind, SPlayerEnt, mind: mind.Comp);
            Server.PlayerMan.SetAttachedEntity(ServerSession, SPlayerEnt);
        });

        await Pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(ServerSession.ContentData()?.Mind, Is.EqualTo(mind.Owner));
            Assert.That(ServerSession.AttachedEntity, Is.EqualTo(SPlayerEnt));
            Assert.That(ServerSession.AttachedEntity, Is.EqualTo(mind.Comp.CurrentEntity),
                "Player is not attached to the mind's current entity.");
            Assert.That(SEntMan.EntityExists(mind.Comp.OwnedEntity),
                "The mind's current entity does not exist");
            Assert.That(mind.Comp.VisitingEntity == null || SEntMan.EntityExists(mind.Comp.VisitingEntity),
                "The minds visited entity does not exist.");
        });
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnDelete()
    {
        Assert.That(SPlayerEnt, Is.Not.EqualTo(null));
        var oldPosition = SEntMan.GetComponent<TransformComponent>(SPlayerEnt).Coordinates;

        Assert.That(!SEntMan.HasComponent<GhostComponent>(SPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await Server.WaitPost(() => SEntMan.DeleteEntity(SPlayerEnt));
        await Pair.RunTicksSync(5);

        var ghost = ServerSession.AttachedEntity!.Value;
        Assert.That(SEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = SEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await Pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnQueueDelete()
    {
        Assert.That(SPlayerEnt, Is.Not.EqualTo(null));
        var oldPosition = SEntMan.GetComponent<TransformComponent>(SPlayerEnt).Coordinates;

        Assert.That(!SEntMan.HasComponent<GhostComponent>(SPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await Server.WaitPost(() => SEntMan.QueueDeleteEntity(SPlayerEnt));
        await Pair.RunTicksSync(5);

        var ghost = ServerSession.AttachedEntity!.Value;
        Assert.That(SEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = SEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await Pair.CleanReturnAsync();
    }

}
