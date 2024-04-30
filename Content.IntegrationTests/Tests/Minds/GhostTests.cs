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
    private TestPair _pair = default!;

    private TestMapData MapData => _pair.TestMap!;

    private RobustIntegrationTest.ServerIntegrationInstance Server => _pair.Server;
    private RobustIntegrationTest.ClientIntegrationInstance Client => _pair.Client;

    /// <summary>
    /// Initial player coordinates. Note that this does not necessarily correspond to the position of the
    /// <see cref="_player"/> entity.
    /// </summary>
    private NetCoordinates _playerCoords;

    private NetEntity _player;
    private EntityUid _sPlayerEnt;

    private ICommonSession _clientSession = default!;
    private ICommonSession _serverSession = default!;

    private IEntityManager _sEntMan;
    private Robust.Server.Player.IPlayerManager _sPlayerMan;
    private Server.Mind.MindSystem _sMindSys;
    private SharedTransformSystem _sTransformSys = default!;

    // [SetUp]
    public async Task Setup()
    {
        // Client is needed to create a session for the ghost system. Creating a dummy session was too difficult.
        _pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        });

        _sEntMan = _pair.Server.ResolveDependency<IServerEntityManager>();
        _sPlayerMan = _pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        _sMindSys = _sEntMan.System<Server.Mind.MindSystem>();
        _sTransformSys = _sEntMan.System<SharedTransformSystem>();

        // Setup map.
        await _pair.CreateTestMap();
        _playerCoords = _sEntMan.GetNetCoordinates(MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)).WithEntityId(MapData.MapUid, _sTransformSys, _sEntMan));

        if (Client.Session == null)
            Assert.Fail("No player");
        _clientSession = Client.Session!;
        _serverSession = _sPlayerMan.GetSessionById(_clientSession.UserId);

        Entity<MindComponent> mind = default!;
        await _pair.Server.WaitPost(() =>
        {
            _player = _sEntMan.GetNetEntity(_sEntMan.SpawnEntity(null, _sEntMan.GetCoordinates(_playerCoords)));
            mind = _sMindSys.CreateMind(_serverSession.UserId, "DummyPlayerEntity");
            _sPlayerEnt = _sEntMan.GetEntity(_player);
            _sMindSys.TransferTo(mind, _sPlayerEnt, mind: mind.Comp);
            Server.PlayerMan.SetAttachedEntity(_serverSession, _sPlayerEnt);
        });

        await _pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(_serverSession.ContentData()?.Mind, Is.EqualTo(mind.Owner));
            Assert.That(_serverSession.AttachedEntity, Is.EqualTo(_sPlayerEnt));
            Assert.That(_serverSession.AttachedEntity, Is.EqualTo(mind.Comp.CurrentEntity),
                "Player is not attached to the mind's current entity.");
            Assert.That(_sEntMan.EntityExists(mind.Comp.OwnedEntity),
                "The mind's current entity does not exist");
            Assert.That(mind.Comp.VisitingEntity == null || _sEntMan.EntityExists(mind.Comp.VisitingEntity),
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
        await Setup();
        Assert.That(_sPlayerEnt, Is.Not.EqualTo(null));
        var oldPosition = _sEntMan.GetComponent<TransformComponent>(_sPlayerEnt).Coordinates;

        Assert.That(!_sEntMan.HasComponent<GhostComponent>(_sPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await Server.WaitPost(() => _sEntMan.DeleteEntity(_sPlayerEnt));
        await _pair.RunTicksSync(5);

        var ghost = _serverSession.AttachedEntity!.Value;
        Assert.That(_sEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = _sEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await _pair.CleanReturnAsync();
    }

    /// <summary>
    /// Test that a ghost gets created when the player entity is deleted.
    /// 1. Delete mob
    /// 2. Assert is ghost
    /// </summary>
    [Test]
    public async Task TestGridGhostOnQueueDelete()
    {
        await Setup();
        Assert.That(_sPlayerEnt, Is.Not.EqualTo(null));
        var oldPosition = _sEntMan.GetComponent<TransformComponent>(_sPlayerEnt).Coordinates;

        Assert.That(!_sEntMan.HasComponent<GhostComponent>(_sPlayerEnt), "Player was initially a ghost?");

        // Delete entity
        await Server.WaitPost(() => _sEntMan.QueueDeleteEntity(_sPlayerEnt));
        await _pair.RunTicksSync(5);

        var ghost = _serverSession.AttachedEntity!.Value;
        Assert.That(_sEntMan.HasComponent<GhostComponent>(ghost), "Player did not become a ghost");

        // Ensure the position is the same
        var ghostPosition = _sEntMan.GetComponent<TransformComponent>(ghost).Coordinates;
        Assert.That(ghostPosition, Is.EqualTo(oldPosition));

        await _pair.CleanReturnAsync();
    }

}
