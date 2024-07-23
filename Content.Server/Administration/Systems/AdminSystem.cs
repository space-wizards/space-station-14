using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
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
        public readonly PanicBunkerStatus PanicBunker = new();
        public readonly BabyJailStatus BabyJail = new();

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _adminManager.OnPermsChanged += OnAdminPermsChanged;
            _playTime.SessionPlayTimeUpdated += OnSessionPlayTimeUpdated;

            // Panic Bunker Settings
            Subs.CVar(_config, CCVars.PanicBunkerEnabled, OnPanicBunkerChanged, true);
            Subs.CVar(_config, CCVars.PanicBunkerDisableWithAdmins, OnPanicBunkerDisableWithAdminsChanged, true);
            Subs.CVar(_config, CCVars.PanicBunkerEnableWithoutAdmins, OnPanicBunkerEnableWithoutAdminsChanged, true);
            Subs.CVar(_config, CCVars.PanicBunkerCountDeadminnedAdmins, OnPanicBunkerCountDeadminnedAdminsChanged, true);
            Subs.CVar(_config, CCVars.PanicBunkerShowReason, OnPanicBunkerShowReasonChanged, true);
            Subs.CVar(_config, CCVars.PanicBunkerMinAccountAge, OnPanicBunkerMinAccountAgeChanged, true);
            Subs.CVar(_config, CCVars.PanicBunkerMinOverallMinutes, OnPanicBunkerMinOverallMinutesChanged, true);

            /*
             * TODO: Remove baby jail code once a more mature gateway process is established. This code is only being issued as a stopgap to help with potential tiding in the immediate future.
             */

            // Baby Jail Settings
            Subs.CVar(_config, CCVars.BabyJailEnabled, OnBabyJailChanged, true);
            Subs.CVar(_config, CCVars.BabyJailShowReason, OnBabyJailShowReasonChanged, true);
            Subs.CVar(_config, CCVars.BabyJailMaxAccountAge, OnBabyJailMaxAccountAgeChanged, true);
            Subs.CVar(_config, CCVars.BabyJailMaxOverallMinutes, OnBabyJailMaxOverallMinutesChanged, true);

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
                RaiseNetworkEvent(updateEv, admin.Channel);
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
                RaiseNetworkEvent(playerInfoChangedEvent, admin.Channel);
            }
        }

        public PlayerInfo? GetCachedPlayerInfo(NetUserId? netUserId)
        {
            if (netUserId == null)
                return null;

            _playerList.TryGetValue(netUserId.Value, out var value);
            return value ?? null;
        }

        private void OnIdentityChanged(ref IdentityChangedEvent ev)
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
                RaiseNetworkEvent(new FullPlayerListEvent(), obj.Player.Channel);
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
            _playTime.SessionPlayTimeUpdated -= OnSessionPlayTimeUpdated;
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

            RaiseNetworkEvent(ev, playerSession.Channel);
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
            PanicBunker.Enabled = enabled;
            _chat.SendAdminAlert(Loc.GetString(enabled
                ? "admin-ui-panic-bunker-enabled-admin-alert"
                : "admin-ui-panic-bunker-disabled-admin-alert"
            ));

            SendPanicBunkerStatusAll();
        }

        private void OnBabyJailChanged(bool enabled)
        {
            BabyJail.Enabled = enabled;
            _chat.SendAdminAlert(Loc.GetString(enabled
                ? "admin-ui-baby-jail-enabled-admin-alert"
                : "admin-ui-baby-jail-disabled-admin-alert"
            ));

            SendBabyJailStatusAll();
        }

        private void OnPanicBunkerDisableWithAdminsChanged(bool enabled)
        {
            PanicBunker.DisableWithAdmins = enabled;
            UpdatePanicBunker();
        }

        private void OnPanicBunkerEnableWithoutAdminsChanged(bool enabled)
        {
            PanicBunker.EnableWithoutAdmins = enabled;
            UpdatePanicBunker();
        }

        private void OnPanicBunkerCountDeadminnedAdminsChanged(bool enabled)
        {
            PanicBunker.CountDeadminnedAdmins = enabled;
            UpdatePanicBunker();
        }

        private void OnPanicBunkerShowReasonChanged(bool enabled)
        {
            PanicBunker.ShowReason = enabled;
            SendPanicBunkerStatusAll();
        }

        private void OnBabyJailShowReasonChanged(bool enabled)
        {
            BabyJail.ShowReason = enabled;
            SendBabyJailStatusAll();
        }

        private void OnPanicBunkerMinAccountAgeChanged(int minutes)
        {
            PanicBunker.MinAccountAgeMinutes = minutes;
            SendPanicBunkerStatusAll();
        }

        private void OnBabyJailMaxAccountAgeChanged(int minutes)
        {
            BabyJail.MaxAccountAgeMinutes = minutes;
            SendBabyJailStatusAll();
        }

        private void OnPanicBunkerMinOverallMinutesChanged(int minutes)
        {
            PanicBunker.MinOverallMinutes = minutes;
            SendPanicBunkerStatusAll();
        }

        private void OnBabyJailMaxOverallMinutesChanged(int minutes)
        {
            BabyJail.MaxOverallMinutes = minutes;
            SendBabyJailStatusAll();
        }

        private void UpdatePanicBunker()
        {
            var admins = PanicBunker.CountDeadminnedAdmins
                ? _adminManager.AllAdmins
                : _adminManager.ActiveAdmins;
            var hasAdmins = admins.Any();

            // TODO Fix order dependent Cvars
            // Please for the sake of my sanity don't make cvars & order dependent.
            // Just make a bool field on the system instead of having some cvars automatically modify other cvars.
            //
            // I.e., this:
            //   /sudo cvar game.panic_bunker.enabled true
            //   /sudo cvar game.panic_bunker.disable_with_admins true
            // and this:
            //   /sudo cvar game.panic_bunker.disable_with_admins true
            //   /sudo cvar game.panic_bunker.enabled true
            //
            // should have the same effect, but currently setting the disable_with_admins can modify enabled.

            if (hasAdmins && PanicBunker.DisableWithAdmins)
            {
                _config.SetCVar(CCVars.PanicBunkerEnabled, false);
            }
            else if (!hasAdmins && PanicBunker.EnableWithoutAdmins)
            {
                _config.SetCVar(CCVars.PanicBunkerEnabled, true);
            }

            SendPanicBunkerStatusAll();
        }

        private void SendPanicBunkerStatusAll()
        {
            var ev = new PanicBunkerChangedEvent(PanicBunker);
            foreach (var admin in _adminManager.AllAdmins)
            {
                RaiseNetworkEvent(ev, admin);
            }
        }

        private void SendBabyJailStatusAll()
        {
            var ev = new BabyJailChangedEvent(BabyJail);
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
                        _stationRecords.TryGetRecord(key, out GeneralStationRecord? record))
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

                        _stationRecords.RemoveRecord(key);
                        Del(item);
                    }
                }

                if (_inventory.TryGetContainerSlotEnumerator(entity.Value, out var enumerator))
                {
                    while (enumerator.NextItem(out var item, out var slot))
                    {
                        if (_inventory.TryUnequip(entity.Value, entity.Value, slot.Name, true, true))
                            _physics.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
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

        private void OnSessionPlayTimeUpdated(ICommonSession session)
        {
            UpdatePlayerList(session);
        }
    }
}
