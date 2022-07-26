using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles
{
    [UsedImplicitly]
    public sealed class GhostRoleSystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private uint _nextRoleIdentifier;
        private bool _needsUpdateGhostRoleCount = true;
        private readonly Dictionary<uint, GhostRoleEntry> _ghostRoles = new();

        private readonly Dictionary<IPlayerSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        // TODO: Reimplement this.
        // [ViewVariables]
        // public IReadOnlyCollection<GhostRoleComponent> GhostRoles => _ghostRoles.Values;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GhostRoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private void OnMobStateChanged(EntityUid uid, GhostRoleComponent component, MobStateChangedEvent args)
        {
            switch (args.CurrentMobState)
            {
                case DamageState.Alive:
                {
                    if (!component.Taken)
                        RegisterGhostRole(component);
                    break;
                }
                case DamageState.Critical:
                case DamageState.Dead:
                    UnregisterGhostRole(component);
                    break;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        }

        private uint GetNextRoleIdentifier()
        {
            return unchecked(_nextRoleIdentifier++);
        }

        public void OpenEui(IPlayerSession session)
        {
            if (session.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.HasComponent<GhostComponent>(attached))
                return;

            if(_openUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openUis[session] = new GhostRolesEui();
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void OpenMakeGhostRoleEui(IPlayerSession session, EntityUid uid)
        {
            if (session.AttachedEntity == null)
                return;

            if (_openMakeGhostRoleUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openMakeGhostRoleUis[session] = new MakeGhostRoleEui(uid);
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(IPlayerSession session)
        {
            if (!_openUis.ContainsKey(session)) return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }

        public void CloseMakeGhostRoleEui(IPlayerSession session)
        {
            if (_openMakeGhostRoleUis.Remove(session, out var eui))
            {
                eui.Close();
            }
        }

        public void UpdateAllEui()
        {
            foreach (var eui in _openUis.Values)
            {
                eui.StateDirty();
            }
            // Note that this, like the EUIs, is deferred.
            // This is for roughly the same reasons, too:
            // Someone might spawn a ton of ghost roles at once.
            _needsUpdateGhostRoleCount = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var needsUpdating = false;

            foreach (var (identifier, entry) in _ghostRoles)
            {
                if (_gameTiming.CurTime < entry.ExpiresAt)
                    continue;

                if (entry.Component != null)
                {
                    ProcessGhostRoleEntryWithRoleComponent(identifier, entry);
                    entry.AddedAt = _gameTiming.CurTime;
                    entry.ExpiresAt = _gameTiming.CurTime + entry.ElapseTime;
                }

                else
                    ProcessGhostRoleEntryWithRequest(identifier, entry);

                needsUpdating = true;
            }

            if (needsUpdating)
                UpdateAllEui();

            if (_needsUpdateGhostRoleCount)
            {
                _needsUpdateGhostRoleCount = false;
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoles.Count);
                foreach (var player in _playerManager.Sessions)
                {
                    RaiseNetworkEvent(response, player.ConnectedClient);
                }
            }
        }

        private void ProcessGhostRoleEntryWithRoleComponent(uint identifier, GhostRoleEntry entry)
        {
            var component = entry.Component;
            var playerCount = entry.PendingPlayerSessions.Count;

            if (playerCount == 0 || component == null)
                return;

            var sessions = new List<IPlayerSession>(entry.PendingPlayerSessions);
            _random.Shuffle(sessions);
            while (sessions.Count > 0)
            {
                var session = sessions.Pop();
                if (session.Status != SessionStatus.InGame || !component.Take(session))
                    continue;

                if (session.AttachedEntity != null)
                    _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{session:player} took the {entry.Component?.RoleName:roleName} ghost role {ToPrettyString(session.AttachedEntity.Value):entity}");

                RemovePlayerTakeoverRequests(session);
                CloseEui(session);
            }
        }

        private void ProcessGhostRoleEntryWithRequest(uint identifier, GhostRoleEntry entry)
        {

        }

        private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.InGame)
            {
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoles.Count);
                RaiseNetworkEvent(response, args.Session.ConnectedClient);
            }
        }

        public void RegisterGhostRole(GhostRoleComponent role)
        {
            if (_ghostRoles.Any(e => e.Value.Component == role))
                return;

            var entry =  GhostRoleEntry.MakeForGhostRole(role);
            entry.AddedAt = _gameTiming.CurTime;
            entry.ExpiresAt = _gameTiming.CurTime + entry.ElapseTime;

            _ghostRoles[role.Identifier = GetNextRoleIdentifier()] = entry;
            UpdateAllEui();
        }

        public void UnregisterGhostRole(GhostRoleComponent role)
        {
            if (!_ghostRoles.ContainsKey(role.Identifier))
                return;

            _ghostRoles.Remove(role.Identifier);
            UpdateAllEui();
        }

        public void RegisterRequest(IGhostRoleRequester requester, int minPlayers, int maxPlayers, TimeSpan timeLimit)
        {
            if (_ghostRoles.Any(v => v.Value.Requester == requester))
                return;

            // _ghostRoles.Add(GetNextRoleIdentifier(), new GhostRoleEntry()
            // {
            //     Requester = requester,
            //     MinimumPlayers = minPlayers,
            //     MaximumPlayers = maxPlayers,
            //     ElapseTime = timeLimit,
            //     ExpiresAt = _gameTiming.CurTime + timeLimit
            // });
            //
            // UpdateAllEui();
        }

        public void UnregisterRequest(IGhostRoleRequester requester)
        {

        }

        public void RequestTakeover(IPlayerSession player, uint identifier)
        {
            if (_ghostRoles.TryGetValue(identifier, out var entry))
            {
                entry.PendingPlayerSessions.Add(player);
                _openUis[player].StateDirty();

                // TODO: Close takeover popup.
            }
        }

        public void CancelTakeover(IPlayerSession player, uint identifier)
        {
            if (_ghostRoles.TryGetValue(identifier, out var entry))
            {
                entry.PendingPlayerSessions.Remove(player);
                _openUis[player].StateDirty();
            }
        }

        public void Follow(IPlayerSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role) || role.Component == null)
                return;

            if (player.AttachedEntity == null)
                return;

            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, role.Component.Owner);
        }

        public void GhostRoleInternalCreateMindAndTransfer(IPlayerSession player, EntityUid roleUid, EntityUid mob, GhostRoleComponent? role = null)
        {
            if (!Resolve(roleUid, ref role))
                return;

            var contentData = player.ContentData();

            DebugTools.AssertNotNull(contentData);

            var newMind = new Mind.Mind(player.UserId)
            {
                CharacterName = EntityManager.GetComponent<MetaDataComponent>(mob).EntityName
            };
            newMind.AddRole(new GhostRoleMarkerRole(newMind, role.RoleName));

            newMind.ChangeOwningPlayer(player.UserId);
            newMind.TransferTo(mob);
        }

        public void RemovePlayerTakeoverRequests(IPlayerSession session)
        {
            foreach (var (identifier, entry) in _ghostRoles)
            {
                entry.PendingPlayerSessions.Remove(session);
            }
        }

        public GhostRoleInfo[] GetGhostRolesInfo(IPlayerSession session)
        {
            var roles = new GhostRoleInfo[_ghostRoles.Count];

            var i = 0;

            foreach (var (id, request) in _ghostRoles)
            {
                roles[i] = new GhostRoleInfo()
                {
                    Identifier = id,
                    Name = request.Name,
                    Description = request.Description,
                    Rules = request.Rules,
                    ExpiresAt = request.ExpiresAt,
                    AddedAt = request.AddedAt,
                    NumSignUps = request.PendingPlayerSessions.Count,
                    IsRequested = request.PendingPlayerSessions.Contains(session),
                };
                i++;
            }

            return roles;
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player)) return;
            if (EntityManager.HasComponent<GhostComponent>(message.Entity)) return;
            CloseEui(message.Player);

            // TODO: Remove player from any pending ghost role requests.
        }

        private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
        {
            component.Taken = true;
            UnregisterGhostRole(component);
        }

        private void OnMindRemoved(EntityUid uid, GhostRoleComponent component, MindRemovedMessage args)
        {
            // Avoid re-registering it for duplicate entries and potential exceptions.
            if (!component.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
                return;

            component.Taken = false;
            RegisterGhostRole(component);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
            _nextRoleIdentifier = 0;
        }

        private void OnInit(EntityUid uid, GhostRoleComponent role, ComponentInit args)
        {
            if (role.Probability < 1f && !_random.Prob(role.Probability))
            {
                RemComp<GhostRoleComponent>(uid);
                return;
            }

            if (role.RoleRules == "")
                role.RoleRules = Loc.GetString("ghost-role-component-default-rules");
            RegisterGhostRole(role);
        }

        private void OnShutdown(EntityUid uid, GhostRoleComponent role, ComponentShutdown args)
        {
            UnregisterGhostRole(role);
        }
    }

    public sealed class GhostRoleEntry
    {
        public string Name = "Role Request Placeholder";

        public string Description = "Testing lottery role allocation";

        public string Rules = "";

        public TimeSpan ElapseTime = TimeSpan.Zero;

        public TimeSpan ExpiresAt = TimeSpan.Zero;

        public TimeSpan AddedAt = TimeSpan.Zero;

        public int MinimumPlayers = 1;

        public int MaximumPlayers = 1;

        public readonly HashSet<IPlayerSession> PendingPlayerSessions = new();

        public GhostRoleComponent? Component = null;

        public IGhostRoleRequester? Requester = null;

        public static GhostRoleEntry MakeForGhostRole(GhostRoleComponent component)
        {
            var inst = new GhostRoleEntry
            {
                Name = component.RoleName,
                Description = component.RoleDescription,
                Rules = component.RoleRules,
                Component = component,
                ElapseTime = TimeSpan.FromSeconds(30),
            };

            return inst;
        }
    }

    [AnyCommand]
    public sealed class GhostRoles : IConsoleCommand
    {
        public string Command => "ghostroles";
        public string Description => "Opens the ghost role request window.";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if(shell.Player != null)
                EntitySystem.Get<GhostRoleSystem>().OpenEui((IPlayerSession)shell.Player);
            else
                shell.WriteLine("You can only open the ghost roles UI on a client.");
        }
    }
}
