using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems
{
    public sealed class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly MindSystem _minds = default!;
        [Dependency] private readonly SharedRoleSystem _role = default!;

        private readonly Dictionary<NetUserId, PlayerInfo> _playerList = new();

        /// <summary>
        ///     Set of players that have participated in this round.
        /// </summary>
        public IReadOnlySet<NetUserId> RoundActivePlayers => _roundActivePlayers;

        private readonly HashSet<NetUserId> _roundActivePlayers = new();

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _adminManager.OnPermsChanged += OnAdminPermsChanged;
            SubscribeLocalEvent<IdentityChangedEvent>(OnIdentityChanged);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoleAddedEvent>(OnRoleEvent);
            SubscribeLocalEvent<RoleRemovedEvent>(OnRoleEvent);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        }

        private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
        {
            _roundActivePlayers.Clear();

            foreach (var (id, data) in _playerList)
            {
                if (!data.ActiveThisRound)
                    continue;

                if (!_playerManager.TryGetPlayerData(id, out var playerData))
                    return;

                _playerManager.TryGetSessionById(id, out var session);
                _playerList[id] = GetPlayerInfo(playerData, session);
            }

            var updateEv = new FullPlayerListEvent() { PlayersInfo = _playerList.Values.ToList() };

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(updateEv, admin.ConnectedClient);
            }
        }

        public void UpdatePlayerList(IPlayerSession player)
        {
            _playerList[player.UserId] = GetPlayerInfo(player.Data, player);

            var playerInfoChangedEvent = new PlayerInfoChangedEvent
            {
                PlayerInfo = _playerList[player.UserId]
            };

            foreach (var admin in _adminManager.ActiveAdmins)
            {
                RaiseNetworkEvent(playerInfoChangedEvent, admin.ConnectedClient);
            }
        }

        public PlayerInfo? GetCachedPlayerInfo(NetUserId? netUserId)
        {
            if (netUserId == null)
                return null;

            _playerList.TryGetValue(netUserId.Value, out var value);
            return value ?? null;
        }

        private void OnIdentityChanged(IdentityChangedEvent ev)
        {
            if (!TryComp<ActorComponent>(ev.CharacterEntity, out var actor))
                return;

            UpdatePlayerList(actor.PlayerSession);
        }

        private void OnRoleEvent(RoleEvent ev)
        {
            var session = _minds.GetSession(ev.Mind);
            if (!ev.Antagonist || session == null)
                return;

            UpdatePlayerList(session);
        }

        private void OnAdminPermsChanged(AdminPermsChangedEventArgs obj)
        {
            if(!obj.IsAdmin)
            {
                RaiseNetworkEvent(new FullPlayerListEvent(), obj.Player.ConnectedClient);
                return;
            }

            SendFullPlayerList(obj.Player);
        }

        private void OnPlayerDetached(PlayerDetachedEvent ev)
        {
            // If disconnected then the player won't have a connected entity to get character name from.
            // The disconnected state gets sent by OnPlayerStatusChanged.
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            UpdatePlayerList(ev.Player);
        }

        private void OnPlayerAttached(PlayerAttachedEvent ev)
        {
            if(ev.Player.Status == SessionStatus.Disconnected) return;

            _roundActivePlayers.Add(ev.Player.UserId);
            UpdatePlayerList(ev.Player);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _adminManager.OnPermsChanged -= OnAdminPermsChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            UpdatePlayerList(e.Session);
        }

        private void SendFullPlayerList(IPlayerSession playerSession)
        {
            var ev = new FullPlayerListEvent();

            ev.PlayersInfo = _playerList.Values.ToList();

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }

        private PlayerInfo GetPlayerInfo(IPlayerData data, IPlayerSession? session)
        {
            var name = data.UserName;
            var entityName = string.Empty;
            var identityName = string.Empty;

            if (session?.AttachedEntity != null)
            {
                entityName = EntityManager.GetComponent<MetaDataComponent>(session.AttachedEntity.Value).EntityName;
                identityName = Identity.Name(session.AttachedEntity.Value, EntityManager);
            }

            var antag = false;
            var startingRole = string.Empty;
            if (_minds.TryGetMind(session, out var mindId, out _))
            {
                antag = _role.MindIsAntagonist(mindId);
                startingRole = _jobs.MindTryGetJobName(mindId);
            }

            var connected = session != null && session.Status is SessionStatus.Connected or SessionStatus.InGame;

            return new PlayerInfo(name, entityName, identityName, startingRole, antag, GetNetEntity(session?.AttachedEntity), data.UserId,
                connected, _roundActivePlayers.Contains(data.UserId));
        }
    }
}
