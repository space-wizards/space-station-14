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
using Content.Shared.Follower.Components;
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

        private bool _needsUpdateGhostRoles = true;
        private bool _needsUpdateGhostRoleCount = true;

        private readonly Dictionary<string, GhostRoleEntry> _ghostRoles = new();

        private int TotalRoleCount => _ghostRoles.Sum(op => op.Value.Components.Count);


        private readonly Dictionary<IPlayerSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        [ViewVariables]
        public IReadOnlyCollection<GhostRoleEntry> GhostRoleEntries => _ghostRoles.Values;


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
            if (!_openUis.ContainsKey(session))
                return;

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

            foreach (var (identifier, entry) in _ghostRoles)
            {
                if (_gameTiming.CurTime < entry.ExpiresAt)
                    continue;

                ProcessGhostRoleEntryWithRoleComponents(identifier, entry);
                if (entry.Components.Count != 0)
                {
                    entry.AddedAt = _gameTiming.CurTime;
                    entry.ExpiresAt = _gameTiming.CurTime + entry.ElapseTime;
                }

                _needsUpdateGhostRoles = true;
            }

            if (_needsUpdateGhostRoles)
            {
                _needsUpdateGhostRoles = false;
                UpdateAllEui();
            }

            if (_needsUpdateGhostRoleCount)
            {
                _needsUpdateGhostRoleCount = false;
                var response = new GhostUpdateGhostRoleCountEvent(TotalRoleCount, _ghostRoles.Keys.ToArray());
                foreach (var player in _playerManager.Sessions)
                {
                    RaiseNetworkEvent(response, player.ConnectedClient);
                }
            }
        }

        private void ProcessGhostRoleEntryWithRoleComponents(string identifier, GhostRoleEntry entry)
        {
            var playerCount = entry.PendingPlayerSessions.Count;

            if (playerCount == 0 || entry.Components.Count == 0)
                return;

            var sessions = new List<IPlayerSession>(entry.PendingPlayerSessions);
            _random.Shuffle(sessions);

            var compIdx = entry.Components.Count - 1; // Ghost role components will unregister themselves via events.
            while (sessions.Count > 0 && compIdx >= 0)
            {
                var session = sessions[^1];
                var component = entry.Components[compIdx];

                if (session.Status != SessionStatus.InGame)
                {
                    sessions.Pop();
                    continue;
                }

                // Remove if already taken or the take-over count has reached the cap.
                if (!component.Take(session))
                {
                    // Assumes that the take over fails only because of the component state.
                    compIdx--;
                    continue;
                }

                if (session.AttachedEntity != null)
                    _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{session:player} took the {entry.Name:roleName} ghost role {ToPrettyString(session.AttachedEntity.Value):entity}");

                sessions.Pop();

                // A single GhostRoleMobSpawnerComponent can spawn multiple entities. Check it is completely used up.
                if(component.Taken)
                    compIdx--;

                CloseEui(session);
            }

            // Clear and add the remaining sessions.
            if (entry.Components.Count > 0)
            {
                entry.PendingPlayerSessions.Clear();
                entry.PendingPlayerSessions.UnionWith(sessions);
            }
        }

        private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.InGame)
            {
                var response = new GhostUpdateGhostRoleCountEvent(TotalRoleCount, _ghostRoles.Keys.ToArray());
                RaiseNetworkEvent(response, args.Session.ConnectedClient);
            }
        }

        public void RegisterGhostRole(GhostRoleComponent role)
        {
            if (!_ghostRoles.TryGetValue(role.RoleName, out var entry))
            {
                var elapseTime = TimeSpan.FromSeconds(30); // TODO: Should this be defined within the role?

                entry = new GhostRoleEntry()
                {
                    Name = role.RoleName,
                    Description = role.RoleDescription,
                    Rules = role.RoleRules,
                    ElapseTime = elapseTime,
                    AddedAt = _gameTiming.CurTime,
                    ExpiresAt = _gameTiming.CurTime + elapseTime,
                };

                _ghostRoles[role.RoleName] = entry;
                _needsUpdateGhostRoles = true;
            }

            if (entry.Components.Contains(role))
                return;

            entry.Components.Add(role);
            role.Identifier = entry.NextComponentIdentifier;
        }

        public void UnregisterGhostRole(GhostRoleComponent role)
        {
            if (!_ghostRoles.TryFirstOrNull(e => e.Value.Name == role.RoleName, out var element))
                return;

            var entry = element.Value.Value;

            entry.Components.Remove(role);
            if (entry.Components.Count == 0)
            {
                _ghostRoles.Remove(element.Value.Key);
                _needsUpdateGhostRoles = true;
            }
        }

        public void RequestTakeover(IPlayerSession player, string identifier)
        {
            if (_ghostRoles.TryGetValue(identifier, out var entry))
            {
                entry.PendingPlayerSessions.Add(player);
                _openUis[player].StateDirty();
            }
        }

        public void CancelTakeover(IPlayerSession player, string identifier)
        {
            if (_ghostRoles.TryGetValue(identifier, out var entry))
            {
                entry.PendingPlayerSessions.Remove(player);
                _openUis[player].StateDirty();
            }
        }

        public void Follow(IPlayerSession player, string roleIdentifier)
        {
            if (player.AttachedEntity == null || !_ghostRoles.TryGetValue(roleIdentifier, out var entry))
                return;

            if (entry.Components.Count == 0)
                return;

            var idx = 0;
            if (TryComp<FollowerComponent>(player.AttachedEntity, out var followerComponent))
            {
                // Get the index of the next entity to follow.
                idx = entry.Components.FindIndex(e => e.Owner == followerComponent.Following) + 1;
                if (idx <= 0 || idx >= entry.Components.Count)
                    idx = 0;
            }

            var component = entry.Components[idx];

            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, component.Owner);
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
            foreach (var (_, entry) in _ghostRoles)
            {
                entry.PendingPlayerSessions.Remove(session);
            }
        }

        public GhostRoleInfo[] GetGhostRolesInfo(IPlayerSession session)
        {
            var roles = new GhostRoleInfo[_ghostRoles.Count];

            var i = 0;

            foreach (var (id, entry) in _ghostRoles)
            {

                roles[i] = new GhostRoleInfo()
                {
                    Name = entry.Name,
                    Description = entry.Description,
                    Rules = entry.Rules,
                    ExpiresAt = entry.ExpiresAt,
                    AddedAt = entry.AddedAt,
                    IsRequested = entry.PendingPlayerSessions.Contains(session),
                    AvailableRoleCount = entry.Components.Count,
                };
                i++;
            }

            return roles;
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player))
                return;
            if (EntityManager.HasComponent<GhostComponent>(message.Entity))
                return;
            CloseEui(message.Player);

            RemovePlayerTakeoverRequests(message.Player);
        }

        private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
        {
            component.Taken = true; // Handle take-overs outside of this system (e.g. Admin take-over).
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
            _ghostRoles.Clear();
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
        [ViewVariables] public string Name = "";

        [ViewVariables] public string Description = "";

        [ViewVariables] public string Rules = "";

        public TimeSpan ElapseTime = TimeSpan.Zero;

        public TimeSpan ExpiresAt = TimeSpan.Zero;

        public TimeSpan AddedAt = TimeSpan.Zero;

        public readonly HashSet<IPlayerSession> PendingPlayerSessions = new();

        [ViewVariables] public readonly List<GhostRoleComponent> Components = new();

        private uint _nextComponentIdentifier;

        public uint NextComponentIdentifier => unchecked(_nextComponentIdentifier++);
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
