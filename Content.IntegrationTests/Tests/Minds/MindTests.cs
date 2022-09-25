using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class MindTests
{

    public const string Prototypes = @"
- type: entity
  id: MindTestEntity
  components:
  - type: Mind
  - type: Damageable
  - type: MobState
    thresholds:
      0: Alive
      100: Critical
      200: Dead
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTypeTrigger
        damageType: Blunt
        damage: 400
        behaviors:
        - !type:GibBehavior { }
";

    private sealed class DummyPlayerManager : IPlayerManager
    {
        public IEnumerable<ICommonSession> NetworkedSessions { get; }
        public IEnumerable<ICommonSession> Sessions { get; }
        public int PlayerCount { get; }
        public int MaxPlayers { get; }
        public BoundKeyMap KeyMap { get; }
        public IEnumerable<IPlayerSession> ServerSessions { get; }
        public event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        private readonly Dictionary<NetUserId, PlayerData> _playerData = new();

        public void AddTestSession(NetUserId userId, String username)
        {
            if (!_playerData.TryGetValue(userId, out var data))
            {
                data = new PlayerData(userId, username);
                _playerData.Add(userId, data);
            }
        }

        public void Initialize(int maxPlayers)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public bool TryGetSessionByUsername(string username, out IPlayerSession? session)
        {
            throw new NotImplementedException();
        }

        public IPlayerSession GetSessionByUserId(NetUserId index)
        {
            throw new NotImplementedException();
        }

        public IPlayerSession GetSessionByChannel(INetChannel channel)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSessionByChannel(INetChannel channel, out IPlayerSession? session)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSessionById(NetUserId userId, out IPlayerSession? session)
        {
            throw new NotImplementedException();
        }

        public bool ValidSessionId(NetUserId index)
        {
            throw new NotImplementedException();
        }

        public IPlayerData GetPlayerData(NetUserId userId)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPlayerData(NetUserId userId, out IPlayerData? data)
        {
            if (_playerData.TryGetValue(userId, out var playerData))
            {
                data = playerData;
                return true;
            }
            data = default;
            return false;
        }

        public bool TryGetPlayerDataByUsername(string userName, out IPlayerData? data)
        {
            throw new NotImplementedException();
        }

        public bool HasPlayerData(NetUserId userId)
        {
            throw new NotImplementedException();
        }

        public bool TryGetUserId(string userName, out NetUserId userId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayerData> GetAllPlayerData()
        {
            throw new NotImplementedException();
        }

        public void DetachAll()
        {
            throw new NotImplementedException();
        }

        public List<IPlayerSession> GetPlayersInRange(MapCoordinates worldPos, int range)
        {
            throw new NotImplementedException();
        }

        public List<IPlayerSession> GetPlayersInRange(EntityCoordinates worldPos, int range)
        {
            throw new NotImplementedException();
        }

        public List<IPlayerSession> GetPlayersBy(Func<IPlayerSession, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public List<IPlayerSession> GetAllPlayers()
        {
            throw new NotImplementedException();
        }

        public List<PlayerState>? GetPlayerStates(GameTick fromTick)
        {
            throw new NotImplementedException();
        }
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        IoCManager.Register<IPlayerManager, DummyPlayerManager>(true);
    }

    [Test]
    public async Task TestCreateAndTransferMind()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true, ExtraPrototypes = Prototypes });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        DummyPlayerManager playerMan = (DummyPlayerManager)server.ResolveDependency<IPlayerManager>();

        await server.WaitAssertion(() =>
        {
            var entity = entMan.SpawnEntity("MindTestEntity", new MapCoordinates());

            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            var userId = new NetUserId(Guid.NewGuid());
            playerMan.AddTestSession(userId, "Tester01");
            var mind = mindSystem.CreateMind(userId);
            mindSystem.ChangeOwningPlayer(mind, userId);
            Assert.That(mind.UserId, Is.EqualTo(userId));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
        });

        // await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }


    [Test]
    public async Task TestEntityDeadWhenGibbed()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        await server.WaitAssertion(() =>
        {

        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    public async Task TestGetPlayerFromEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        await server.WaitAssertion(() =>
        {
            // var playerSession = new PlayerSession();

            var entity = entMan.SpawnEntity("MindTestEntity", new EntityCoordinates());

            var mindSys = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            // mindComp.Mind?.Session;
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        await server.WaitAssertion(() =>
        {
            var entity = entMan.SpawnEntity("MindTestEntity", new EntityCoordinates());
            var targetEntity = entMan.SpawnEntity("MindTestEntity", new EntityCoordinates());

            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            var mind = mindSystem.CreateMind(new NetUserId(Guid.Parse("1")));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            mindSystem.TransferTo(mind, targetEntity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(null));
            Assert.That(mindSystem.GetMind(targetEntity, mindComp), Is.EqualTo(mind));
        });

        // await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }
}