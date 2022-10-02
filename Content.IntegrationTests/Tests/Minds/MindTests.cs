#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class MindTests
{
    private const string Prototypes = @"
- type: entity
  id: MindTestEntity
  components:
  - type: Mind

- type: entity
  parent: MindTestEntity
  id: MindTestEntityDamageable
  components:
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
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IServerNetManager _network = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public BoundKeyMap KeyMap { get; private set; } = default!;

        private GameTick _lastStateUpdate;

        private readonly ReaderWriterLockSlim _sessionsLock = new();

        /// <summary>
        ///     Active sessions of connected clients to the server.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<NetUserId, PlayerSession> _sessions = new();

        [ViewVariables]
        private readonly Dictionary<NetUserId, PlayerData> _playerData = new();

        [ViewVariables]
        private readonly Dictionary<string, NetUserId> _userIdMap = new();

        /// <inheritdoc />
        public IEnumerable<ICommonSession> NetworkedSessions => Sessions;

        /// <inheritdoc />
        public IEnumerable<ICommonSession> Sessions
        {
            get
            {
                _sessionsLock.EnterReadLock();
                try
                {
                    return _sessions.Values;
                }
                finally
                {
                    _sessionsLock.ExitReadLock();
                }
            }
        }

        public IEnumerable<IPlayerSession> ServerSessions => Sessions.Cast<IPlayerSession>();

        /// <inheritdoc />
        [ViewVariables]
        public int PlayerCount
        {
            get
            {
                _sessionsLock.EnterReadLock();
                try
                {
                    return _sessions.Count;
                }
                finally
                {
                    _sessionsLock.ExitReadLock();
                }
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public int MaxPlayers { get; private set; } = 32;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs>? PlayerStatusChanged;

        /// <inheritdoc />
        public void Initialize(int maxPlayers)
        {
            KeyMap = new BoundKeyMap(_reflectionManager);
            KeyMap.PopulateKeyFunctionsMap();

            MaxPlayers = maxPlayers;

            _network.RegisterNetMessage<MsgPlayerListReq>(HandlePlayerListReq);
        }

        public void Shutdown()
        {
            KeyMap = default!;
        }

        public bool TryGetSessionByUsername(string username, [NotNullWhen(true)] out IPlayerSession? session)
        {
            if (!_userIdMap.TryGetValue(username, out var userId))
            {
                session = null;
                return false;
            }

            _sessionsLock.EnterReadLock();
            try
            {
                if (_sessions.TryGetValue(userId, out var iSession))
                {
                    session = iSession;
                    return true;
                }
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }


            session = null;
            return false;
        }

        IPlayerSession IPlayerManager.GetSessionByChannel(INetChannel channel) => GetSessionByChannel(channel);
        public bool TryGetSessionByChannel(INetChannel channel, [NotNullWhen(true)] out IPlayerSession? session)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                // Should only be one session per client. Returns that session, in theory.
                if (_sessions.TryGetValue(channel.UserId, out var concrete))
                {
                    session = concrete;
                    return true;
                }

                session = null;
                return false;
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        private PlayerSession GetSessionByChannel(INetChannel channel)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                // Should only be one session per client. Returns that session, in theory.
                return _sessions[channel.UserId];
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IPlayerSession GetSessionByUserId(NetUserId index)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions[index];
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public bool ValidSessionId(NetUserId index)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions.ContainsKey(index);
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public bool TryGetSessionById(NetUserId userId, [NotNullWhen(true)] out IPlayerSession? session)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                if (_sessions.TryGetValue(userId, out var playerSession))
                {
                    session = playerSession;
                    return true;
                }
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
            session = default;
            return false;
        }

        public bool TryGetUserId(string userName, out NetUserId userId)
        {
            return _userIdMap.TryGetValue(userName, out userId);
        }

        public IEnumerable<IPlayerData> GetAllPlayerData()
        {
            return _playerData.Values;
        }

        /// <summary>
        ///     Causes all sessions to detach from their entity.
        /// </summary>
        [Obsolete]
        public void DetachAll()
        {
            _sessionsLock.EnterReadLock();
            try
            {
                foreach (var s in _sessions.Values)
                {
                    s.DetachFromEntity();
                }
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Gets all players inside of a circle.
        /// </summary>
        /// <param name="worldPos">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        [Obsolete("Use player Filter or Inline me!")]
        public List<IPlayerSession> GetPlayersInRange(MapCoordinates worldPos, int range)
        {
            return Filter.Empty()
                .AddInRange(worldPos, range)
                .Recipients
                .Cast<IPlayerSession>()
                .ToList();
        }

        /// <summary>
        ///     Gets all players inside of a circle.
        /// </summary>
        /// <param name="worldPos">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        [Obsolete("Use player Filter or Inline me!")]
        public List<IPlayerSession> GetPlayersInRange(EntityCoordinates worldPos, int range)
        {
            return Filter.Empty()
                .AddInRange(worldPos.ToMap(_entityManager), range)
                .Recipients
                .Cast<IPlayerSession>()
                .ToList();
        }

        [Obsolete("Use player Filter or Inline me!")]
        public List<IPlayerSession> GetPlayersBy(Func<IPlayerSession, bool> predicate)
        {
            return Filter.Empty()
                .AddWhere((session => predicate((IPlayerSession)session)))
                .Recipients
                .Cast<IPlayerSession>()
                .ToList();
        }

        /// <summary>
        ///     Gets all players in the server.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use player Filter or Inline me!")]
        public List<IPlayerSession> GetAllPlayers()
        {
            return ServerSessions.ToList();
        }

        /// <summary>
        ///     Gets all player states in the server.
        /// </summary>
        /// <param name="fromTick"></param>
        /// <returns></returns>
        public List<PlayerState>? GetPlayerStates(GameTick fromTick)
        {
            if (_lastStateUpdate < fromTick)
            {
                return null;
            }

            _sessionsLock.EnterReadLock();
            try
            {
                return _sessions.Values
                    .Select(s => s.PlayerState)
                    .ToList();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Creates a new session for a client.
        /// </summary>
        internal void NewSession(NetUserId userId, string username)
        {
            if (!_playerData.TryGetValue(userId, out var data))
            {
                data = new PlayerData(userId, username);
                _playerData.Add(userId, data);
            }

            _userIdMap[username] = userId;
        }

        private void HandlePlayerListReq(MsgPlayerListReq message)
        {
            var channel = message.MsgChannel;
            var players = Sessions;
            var netMsg = new MsgPlayerList();

            var list = new List<PlayerState>();
            foreach (var client in players)
            {
                var info = new PlayerState
                {
                    UserId = client.UserId,
                    Name = client.Name,
                    Status = client.Status,
                    Ping = client.ConnectedClient.Ping
                };
                list.Add(info);
            }
            netMsg.Plyrs = list;
            netMsg.PlyCount = (byte)list.Count;

            channel.SendMessage(netMsg);
        }

        public void Dirty()
        {
            _lastStateUpdate = _timing.CurTick;
        }

        public IPlayerData GetPlayerData(NetUserId userId)
        {
            return _playerData[userId];
        }

        public bool TryGetPlayerData(NetUserId userId, [NotNullWhen(true)] out IPlayerData? data)
        {
            if (_playerData.TryGetValue(userId, out var playerData))
            {
                data = playerData;
                return true;
            }
            data = default;
            return false;
        }

        public bool TryGetPlayerDataByUsername(string userName, [NotNullWhen(true)] out IPlayerData? data)
        {
            if (!_userIdMap.TryGetValue(userName, out var userId))
            {
                data = null;
                return false;
            }

            // PlayerData is initialized together with the _userIdMap so we can trust that it'll be present.
            data = _playerData[userId];
            return true;
        }

        public bool HasPlayerData(NetUserId userId)
        {
            return _playerData.ContainsKey(userId);
        }
    }

    // Exception handling for PlayerData and NetUserId invalid due to testing.
    // Can be removed when Players can be mocked.
    private Mind CreateMind(NetUserId userId, MindSystem mindSystem)
    {
        Mind? mind = null;

        CatchPlayerDataException(() =>
            mindSystem.TryCreateMind(userId, out mind));

        Assert.NotNull(mind);
        return mind!;
    }

    /// <summary>
    ///     Exception handling for PlayerData and NetUserId invalid due to testing.
    ///     Can be removed when Players can be mocked.
    /// </summary>
    /// <param name="func"></param>
    private void CatchPlayerDataException(Action func)
    {
        try
        {
            func();
        }
        catch (ArgumentException e)
        {
            // Prevent exiting due to PlayerData not being initialized.
            if (e.Message == "New owner must have previously logged into the server. (Parameter 'newOwner')")
                return;
            throw;
        }
    }

    [Test]
    public async Task TestCreateAndTransferMind()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = new DummyPlayerManager();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindComponent>(entity);
            var userId = new NetUserId(Guid.NewGuid());
            playerMan.NewSession(userId, "JoeGenero");

            var mind = CreateMind(userId, mindSystem);

            Assert.That(mind.UserId, Is.EqualTo(userId));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestEntityDeadWhenGibbed()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true, ExtraPrototypes = Prototypes });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        EntityUid entity = default!;
        MindComponent mindComp = default!;
        Mind mind = default!;
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();
        var damageableSystem = entMan.EntitySysManager.GetEntitySystem<DamageableSystem>();
        var playerMan = new DummyPlayerManager();

        await server.WaitAssertion(() =>
        {
            entity = entMan.SpawnEntity("MindTestEntityDamageable", new MapCoordinates());
            mindComp = entMan.EnsureComponent<MindComponent>(entity);
            var userId = new NetUserId(Guid.NewGuid());
            playerMan.NewSession(userId, "JoeGenero");

            mind = CreateMind(userId, mindSystem);

            Assert.That(mind.UserId, Is.EqualTo(userId));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.GetComponent<DamageableComponent>(entity);
            if (!protoMan.TryIndex<DamageTypePrototype>("Blunt", out var prototype))
            {
                return;
            }

            damageableSystem.SetDamage(damageable, new DamageSpecifier(prototype, FixedPoint2.New(401)));
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
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

            var entity = entMan.SpawnEntity(null, new MapCoordinates());

            var mindSys = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var mindComp = entMan.GetComponent<MindComponent>(entity);
            // mindComp.Mind?.Session;
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var targetEntity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindComponent>(entity);
            entMan.EnsureComponent<MindComponent>(targetEntity);

            var mind = CreateMind(new NetUserId(Guid.NewGuid()), mindSystem);

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            mindSystem.TransferTo(mind, targetEntity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(null));
            Assert.That(mindSystem.GetMind(targetEntity), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestOwningPlayerCanBeChanged()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = new DummyPlayerManager();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindComponent>(entity);

            var userId = new NetUserId(Guid.NewGuid());
            playerMan.NewSession(userId, "JoeGenero");
            var mind = CreateMind(userId, mindSystem);

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
            Assert.That(mind.UserId, Is.EqualTo(userId));

            var newUserId = new NetUserId(Guid.NewGuid());
            playerMan.NewSession(userId, "JaneGenero");
            CatchPlayerDataException(() =>
                mindSystem.ChangeOwningPlayer(entity, newUserId, mindComp));

            Assert.That(mind.UserId, Is.EqualTo(newUserId));
        });

        await pairTracker.CleanReturnAsync();
    }
}