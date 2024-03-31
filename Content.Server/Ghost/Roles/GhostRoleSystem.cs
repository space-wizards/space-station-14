using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;
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
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;

        private uint _nextRoleIdentifier;
        private bool _needsUpdateGhostRoleCount = true;
        private readonly Dictionary<uint, Entity<GhostRoleComponent>> _ghostRoles = new();
        private readonly Dictionary<ICommonSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<ICommonSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        [ViewVariables]
        public IReadOnlyCollection<Entity<GhostRoleComponent>> GhostRoles => _ghostRoles.Values;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GhostRoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GhostRoleComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<GhostRoleComponent, EntityPausedEvent>(OnPaused);
            SubscribeLocalEvent<GhostRoleComponent, EntityUnpausedEvent>(OnUnpaused);
            SubscribeLocalEvent<GhostRoleMobSpawnerComponent, TakeGhostRoleEvent>(OnSpawnerTakeRole);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, TakeGhostRoleEvent>(OnTakeoverTakeRole);
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private void OnMobStateChanged(Entity<GhostTakeoverAvailableComponent> component, ref MobStateChangedEvent args)
        {
            if (!TryComp(component, out GhostRoleComponent? ghostRole))
                return;

            switch (args.NewMobState)
            {
                case MobState.Alive:
                {
                    if (!ghostRole.Taken)
                        RegisterGhostRole((component, ghostRole));
                    break;
                }
                case MobState.Critical:
                case MobState.Dead:
                    UnregisterGhostRole((component, ghostRole));
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

        public void OpenEui(ICommonSession session)
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

        public void OpenMakeGhostRoleEui(ICommonSession session, EntityUid uid)
        {
            if (session.AttachedEntity == null)
                return;

            if (_openMakeGhostRoleUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openMakeGhostRoleUis[session] = new MakeGhostRoleEui(EntityManager, GetNetEntity(uid));
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(ICommonSession session)
        {
            if (!_openUis.ContainsKey(session))
                return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }

        public void CloseMakeGhostRoleEui(ICommonSession session)
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
            if (_needsUpdateGhostRoleCount)
            {
                _needsUpdateGhostRoleCount = false;
                var response = new GhostUpdateGhostRoleCountEvent(GetGhostRolesInfo().Length);
                foreach (var player in _playerManager.Sessions)
                {
                    RaiseNetworkEvent(response, player.Channel);
                }
            }
        }

        private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.InGame)
            {
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoles.Count);
                RaiseNetworkEvent(response, args.Session.Channel);
            }
        }

        public void RegisterGhostRole(Entity<GhostRoleComponent> role)
        {
            if (_ghostRoles.ContainsValue(role))
                return;

            _ghostRoles[role.Comp.Identifier = GetNextRoleIdentifier()] = role;
            UpdateAllEui();
        }

        public void UnregisterGhostRole(Entity<GhostRoleComponent> role)
        {
            var comp = role.Comp;
            if (!_ghostRoles.ContainsKey(comp.Identifier) || _ghostRoles[comp.Identifier] != role)
                return;

            _ghostRoles.Remove(comp.Identifier);
            UpdateAllEui();
        }

        public void Takeover(ICommonSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role))
                return;

            var ev = new TakeGhostRoleEvent(player);
            RaiseLocalEvent(role, ref ev);

            if (!ev.TookRole)
                return;

            if (player.AttachedEntity != null)
                _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {role.Comp.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");

            CloseEui(player);
        }

        public void Follow(ICommonSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role))
                return;

            if (player.AttachedEntity == null)
                return;

            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, role);
        }

        public void GhostRoleInternalCreateMindAndTransfer(ICommonSession player, EntityUid roleUid, EntityUid mob, GhostRoleComponent? role = null)
        {
            if (!Resolve(roleUid, ref role))
                return;

            DebugTools.AssertNotNull(player.ContentData());

            var newMind = _mindSystem.CreateMind(player.UserId,
                EntityManager.GetComponent<MetaDataComponent>(mob).EntityName);
            _roleSystem.MindAddRole(newMind, new GhostRoleMarkerRoleComponent { Name = role.RoleName });

            _mindSystem.SetUserId(newMind, player.UserId);
            _mindSystem.TransferTo(newMind, mob);
        }

        public GhostRoleInfo[] GetGhostRolesInfo()
        {
            var roles = new List<GhostRoleInfo>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();

            foreach (var (id, (uid, role)) in _ghostRoles)
            {
                if (metaQuery.GetComponent(uid).EntityPaused)
                    continue;

                roles.Add(new GhostRoleInfo {Identifier = id, Name = role.RoleName, Description = role.RoleDescription, Rules = role.RoleRules, Requirements = role.Requirements});
            }

            return roles.ToArray();
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player))
                return;

            if (HasComp<GhostComponent>(message.Entity))
                return;

            CloseEui(message.Player);
        }

        private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            ghostRole.Taken = true;
            UnregisterGhostRole((uid, ghostRole));
        }

        private void OnMindRemoved(EntityUid uid, GhostTakeoverAvailableComponent component, MindRemovedMessage args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            // Avoid re-registering it for duplicate entries and potential exceptions.
            if (!ghostRole.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
                return;

            ghostRole.Taken = false;
            RegisterGhostRole((uid, ghostRole));
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
            _ghostRoles.Clear();
            _nextRoleIdentifier = 0;
        }

        private void OnPaused(EntityUid uid, GhostRoleComponent component, ref EntityPausedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            UpdateAllEui();
        }

        private void OnUnpaused(EntityUid uid, GhostRoleComponent component, ref EntityUnpausedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            UpdateAllEui();
        }

        private void OnMapInit(Entity<GhostRoleComponent> ent, ref MapInitEvent args)
        {
            if (ent.Comp.Probability < 1f && !_random.Prob(ent.Comp.Probability))
                RemCompDeferred<GhostRoleComponent>(ent);
        }

        private void OnStartup(Entity<GhostRoleComponent> ent, ref ComponentStartup args)
        {
            RegisterGhostRole(ent);
        }

        private void OnShutdown(Entity<GhostRoleComponent> role, ref ComponentShutdown args)
        {
            UnregisterGhostRole(role);
        }

        private void OnSpawnerTakeRole(EntityUid uid, GhostRoleMobSpawnerComponent component, ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                !CanTakeGhost(uid, ghostRole))
            {
                args.TookRole = false;
                return;
            }

            if (string.IsNullOrEmpty(component.Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var mob = Spawn(component.Prototype, Transform(uid).Coordinates);
            _transform.AttachToGridOrMap(mob);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
            RaiseLocalEvent(mob, spawnedEvent);

            if (ghostRole.MakeSentient)
                MakeSentientCommand.MakeSentient(mob, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

            EnsureComp<MindContainerComponent>(mob);

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                args.TookRole = true;
                return;
            }

            ghostRole.Taken = true;

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            args.TookRole = true;
        }

        private bool CanTakeGhost(EntityUid uid, GhostRoleComponent? component = null)
        {
            return Resolve(uid, ref component, false) &&
                   !component.Taken &&
                   !MetaData(uid).EntityPaused;
        }

        private void OnTakeoverTakeRole(EntityUid uid, GhostTakeoverAvailableComponent component, ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                !CanTakeGhost(uid, ghostRole))
            {
                args.TookRole = false;
                return;
            }

            ghostRole.Taken = true;

            var mind = EnsureComp<MindContainerComponent>(uid);

            if (mind.HasMind)
            {
                args.TookRole = false;
                return;
            }

            if (ghostRole.MakeSentient)
                MakeSentientCommand.MakeSentient(uid, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, uid, ghostRole);
            UnregisterGhostRole((uid, ghostRole));

            args.TookRole = true;
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
                EntitySystem.Get<GhostRoleSystem>().OpenEui(shell.Player);
            else
                shell.WriteLine("You can only open the ghost roles UI on a client.");
        }
    }
}
