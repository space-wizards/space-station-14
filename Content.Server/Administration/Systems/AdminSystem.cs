using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems
{
    public sealed class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly HandsSystem _hands = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly MindSystem _minds = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;
        [Dependency] private readonly SharedRoleSystem _role = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        private readonly Dictionary<NetUserId, PlayerInfo> _playerList = new();

        /// <summary>
        ///     Set of players that have participated in this round.
        /// </summary>
        public IReadOnlySet<NetUserId> RoundActivePlayers => _roundActivePlayers;

        private readonly HashSet<NetUserId> _roundActivePlayers = new();
        private readonly PanicBunkerStatus _panicBunker = new();

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _adminManager.OnPermsChanged += OnAdminPermsChanged;

            _config.OnValueChanged(CCVars.PanicBunkerEnabled, OnPanicBunkerChanged, true);
            _config.OnValueChanged(CCVars.PanicBunkerDisableWithAdmins, OnPanicBunkerDisableWithAdminsChanged, true);
            _config.OnValueChanged(CCVars.PanicBunkerEnableWithoutAdmins, OnPanicBunkerEnableWithoutAdminsChanged, true);
            _config.OnValueChanged(CCVars.PanicBunkerCountDeadminnedAdmins, OnPanicBunkerCountDeadminnedAdminsChanged, true);
            _config.OnValueChanged(CCVars.PanicBunkerShowReason, OnShowReasonChanged, true);
            _config.OnValueChanged(CCVars.PanicBunkerMinAccountAge, OnPanicBunkerMinAccountAgeChanged, true);
            _config.OnValueChanged(CCVars.PanicBunkerMinOverallHours, OnPanicBunkerMinOverallHoursChanged, true);

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

        public void UpdatePlayerList(ICommonSession player)
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
            UpdatePanicBunker();

            if (!obj.IsAdmin)
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
            if (ev.Player.Status == SessionStatus.Disconnected)
                return;

            UpdatePlayerList(ev.Player);
        }

        private void OnPlayerAttached(PlayerAttachedEvent ev)
        {
            if (ev.Player.Status == SessionStatus.Disconnected)
                return;

            _roundActivePlayers.Add(ev.Player.UserId);
            UpdatePlayerList(ev.Player);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _adminManager.OnPermsChanged -= OnAdminPermsChanged;

            _config.UnsubValueChanged(CCVars.PanicBunkerEnabled, OnPanicBunkerChanged);
            _config.UnsubValueChanged(CCVars.PanicBunkerDisableWithAdmins, OnPanicBunkerDisableWithAdminsChanged);
            _config.UnsubValueChanged(CCVars.PanicBunkerEnableWithoutAdmins, OnPanicBunkerEnableWithoutAdminsChanged);
            _config.UnsubValueChanged(CCVars.PanicBunkerCountDeadminnedAdmins, OnPanicBunkerCountDeadminnedAdminsChanged);
            _config.UnsubValueChanged(CCVars.PanicBunkerShowReason, OnShowReasonChanged);
            _config.UnsubValueChanged(CCVars.PanicBunkerMinAccountAge, OnPanicBunkerMinAccountAgeChanged);
            _config.UnsubValueChanged(CCVars.PanicBunkerMinOverallHours, OnPanicBunkerMinOverallHoursChanged);
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            UpdatePlayerList(e.Session);
            UpdatePanicBunker();
        }

        private void SendFullPlayerList(ICommonSession playerSession)
        {
            var ev = new FullPlayerListEvent();

            ev.PlayersInfo = _playerList.Values.ToList();

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
        }

        private PlayerInfo GetPlayerInfo(SessionData data, ICommonSession? session)
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
            TimeSpan? overallPlaytime = null;
            if (session != null &&
                _playTime.TryGetTrackerTimes(session, out var playTimes) &&
                playTimes.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out var playTime))
            {
                overallPlaytime = playTime;
            }

            return new PlayerInfo(name, entityName, identityName, startingRole, antag, GetNetEntity(session?.AttachedEntity), data.UserId,
                connected, _roundActivePlayers.Contains(data.UserId), overallPlaytime);
        }

        private void OnPanicBunkerChanged(bool enabled)
        {
            _panicBunker.Enabled = enabled;
            _chat.SendAdminAlert(Loc.GetString(enabled
                ? "admin-ui-panic-bunker-enabled-admin-alert"
                : "admin-ui-panic-bunker-disabled-admin-alert"
            ));

            SendPanicBunkerStatusAll();
        }

        private void OnPanicBunkerDisableWithAdminsChanged(bool enabled)
        {
            _panicBunker.DisableWithAdmins = enabled;
            UpdatePanicBunker();
        }

        private void OnPanicBunkerEnableWithoutAdminsChanged(bool enabled)
        {
            _panicBunker.EnableWithoutAdmins = enabled;
            UpdatePanicBunker();
        }

        private void OnPanicBunkerCountDeadminnedAdminsChanged(bool enabled)
        {
            _panicBunker.CountDeadminnedAdmins = enabled;
            UpdatePanicBunker();
        }

        private void OnShowReasonChanged(bool enabled)
        {
            _panicBunker.ShowReason = enabled;
            SendPanicBunkerStatusAll();
        }

        private void OnPanicBunkerMinAccountAgeChanged(int minutes)
        {
            _panicBunker.MinAccountAgeHours = minutes / 60;
            SendPanicBunkerStatusAll();
        }

        private void OnPanicBunkerMinOverallHoursChanged(int hours)
        {
            _panicBunker.MinOverallHours = hours;
            SendPanicBunkerStatusAll();
        }

        private void UpdatePanicBunker()
        {
            var admins = _panicBunker.CountDeadminnedAdmins
                ? _adminManager.AllAdmins
                : _adminManager.ActiveAdmins;
            var hasAdmins = admins.Any();

            if (hasAdmins && _panicBunker.DisableWithAdmins)
            {
                _config.SetCVar(CCVars.PanicBunkerEnabled, false);
            }
            else if (!hasAdmins && _panicBunker.EnableWithoutAdmins)
            {
                _config.SetCVar(CCVars.PanicBunkerEnabled, true);
            }

            SendPanicBunkerStatusAll();
        }

        private void SendPanicBunkerStatusAll()
        {
            var ev = new PanicBunkerChangedEvent(_panicBunker);
            foreach (var admin in _adminManager.AllAdmins)
            {
                RaiseNetworkEvent(ev, admin);
            }
        }

        /// <summary>
        ///     Erases a player from the round.
        ///     This removes them and any trace of them from the round, deleting their
        ///     chat messages and showing a popup to other players.
        ///     Their items are dropped on the ground.
        /// </summary>
        public void Erase(ICommonSession player)
        {
            var entity = player.AttachedEntity;
            _chat.DeleteMessagesBy(player);

            if (entity != null && !TerminatingOrDeleted(entity.Value))
            {
                if (TryComp(entity.Value, out TransformComponent? transform))
                {
                    var coordinates = _transform.GetMoverCoordinates(entity.Value, transform);
                    var name = Identity.Entity(entity.Value, EntityManager);
                    _popup.PopupCoordinates(Loc.GetString("admin-erase-popup", ("user", name)), coordinates, PopupType.LargeCaution);
                    var filter = Filter.Pvs(coordinates, 1, EntityManager, _playerManager);
                    var audioParams = new AudioParams().WithVolume(3);
                    _audio.PlayStatic("/Audio/Effects/pop_high.ogg", filter, coordinates, true, audioParams);
                }

                foreach (var item in _inventory.GetHandOrInventoryEntities(entity.Value))
                {
                    if (TryComp(item, out PdaComponent? pda) &&
                        TryComp(pda.ContainedId, out StationRecordKeyStorageComponent? keyStorage) &&
                        keyStorage.Key is { } key &&
                        _stationRecords.TryGetRecord(key.OriginStation, key, out GeneralStationRecord? record))
                    {
                        if (TryComp(entity, out DnaComponent? dna) &&
                            dna.DNA != record.DNA)
                        {
                            continue;
                        }

                        if (TryComp(entity, out FingerprintComponent? fingerPrint) &&
                            fingerPrint.Fingerprint != record.Fingerprint)
                        {
                            continue;
                        }

                        _stationRecords.RemoveRecord(key.OriginStation, key);
                        Del(item);
                    }
                }

                if (TryComp(entity.Value, out InventoryComponent? inventory) &&
                    _inventory.TryGetSlots(entity.Value, out var slots, inventory))
                {
                    foreach (var slot in slots)
                    {
                        if (_inventory.TryUnequip(entity.Value, entity.Value, slot.Name, out var item, true, true))
                        {
                            _physics.ApplyAngularImpulse(item.Value, ThrowingSystem.ThrowAngularImpulse);
                        }
                    }
                }

                if (TryComp(entity.Value, out HandsComponent? hands))
                {
                    foreach (var hand in _hands.EnumerateHands(entity.Value, hands))
                    {
                        _hands.TryDrop(entity.Value, hand, checkActionBlocker: false, doDropInteraction: false, handsComp: hands);
                    }
                }
            }

            _minds.WipeMind(player);
            QueueDel(entity);

            _gameTicker.SpawnObserver(player);
        }
    }
}
