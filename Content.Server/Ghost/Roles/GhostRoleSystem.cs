using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.AI.Components;
using Content.Server.AI.EntitySystems;
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
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
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
        [Dependency] private readonly GhostRoleManager _ghostRoleManager = default!;
        [Dependency] private readonly NPCSystem _npcSystem = default!;

        private bool _needsUpdateGhostRoles = true;
        private bool _needsUpdateGhostRoleCount = true;

        private readonly Dictionary<IPlayerSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        // [ViewVariables]
        // public IReadOnlyCollection<GhostRoleComponent> GhostRoleEntries => _ghostRoles.Values;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GhostRoleComponent, EntityPlacedEvent>(OnEntityPlaced);
            SubscribeLocalEvent<GhostRoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
            _ghostRoleManager.OnGhostRolesChanged += OnGhostRolesChanged;
            _ghostRoleManager.OnPlayerTakeoverComplete += OnPlayerTakeoverComplete;
        }

        private void OnEntityPlaced(EntityUid uid, GhostRoleComponent component, EntityPlacedEvent args)
        {
            if (_ghostRoleManager.TryAttachToActiveGhostRoleGroup(args.PlacedBy, component))
            {
                Logger.Debug($"Added {ToPrettyString(args.Placed)} to role group.");
                if(TryComp<NPCComponent>(uid, out var npc))
                    _npcSystem.SleepNPC(npc); // Prevent mobs moving about while setting up event.
            }
        }

        private void OnMobStateChanged(EntityUid uid, GhostRoleComponent component, MobStateChangedEvent args)
        {
            switch (args.CurrentMobState)
            {
                case DamageState.Alive:
                {
                    if (!component.Taken)
                        _ghostRoleManager.QueueGhostRole(component);
                    break;
                }
                case DamageState.Critical:
                case DamageState.Dead:
                    _ghostRoleManager.RemoveGhostRole(component);
                    break;
            }
        }

        private void OnGhostRolesChanged(GhostRolesChangedEventArgs e)
        {
            if (e.UpdateSession != null && _openUis.TryGetValue(e.UpdateSession, out var ui))
            {
                ui.StateDirty();
                return;
            }

            _needsUpdateGhostRoles = true;
            _needsUpdateGhostRoleCount = true;
        }

        private void OnPlayerTakeoverComplete(PlayerTakeoverCompleteEventArgs e)
        {
            if (e.Session.AttachedEntity != null)
                _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{e.Session:player} took the {e.RoleName:roleName} ghost role {ToPrettyString(e.Session.AttachedEntity.Value):entity}");

            _ghostRoleManager.ClearPlayerRequests(e.Session);
            CloseEui(e.Session);
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
            _ghostRoleManager.Update();

            UpdateUi();
        }

        private void UpdateUi()
        {
            if (_needsUpdateGhostRoles)
            {
                _needsUpdateGhostRoles = false;
                UpdateAllEui();
            }

            if (_needsUpdateGhostRoleCount)
            {
                _needsUpdateGhostRoleCount = false;
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoleManager.AvailableRolesCount, _ghostRoleManager.AvailableRoles);
                foreach (var player in _playerManager.Sessions)
                {
                    RaiseNetworkEvent(response, player.ConnectedClient);
                }
            }
        }

        private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.InGame)
            {
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoleManager.AvailableRolesCount, _ghostRoleManager.AvailableRoles);
                RaiseNetworkEvent(response, args.Session.ConnectedClient);
            }
        }

        public void Follow(IPlayerSession player, string roleIdentifier)
        {
            if (player.AttachedEntity == null)
                return;

            GhostRoleComponent? next = null;
            if (TryComp<FollowerComponent>(player.AttachedEntity, out var followerComponent) &&
                TryComp<GhostRoleComponent>(followerComponent.Following, out var ghostRoleComponent))
            {
                next = _ghostRoleManager.GetNextGhostRoleComponentOrNull(roleIdentifier, ghostRoleComponent.Identifier);
            }

            if (next == null && !_ghostRoleManager.TryGetFirstGhostRoleComponent(roleIdentifier, out next))
                return;

            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, next.Owner);
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

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player))
                return;
            if (EntityManager.HasComponent<GhostComponent>(message.Entity))
                return;

            CloseEui(message.Player);
            _ghostRoleManager.ClearPlayerRequests(message.Player);
        }

        private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
        {
            component.Taken = true; // Handle take-overs outside of this system (e.g. Admin take-over).
            _ghostRoleManager.RemoveGhostRole(component);
        }

        private void OnMindRemoved(EntityUid uid, GhostRoleComponent component, MindRemovedMessage args)
        {
            // Avoid re-registering it for duplicate entries and potential exceptions.
            if (!component.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
                return;

            component.Taken = false;
            _ghostRoleManager.QueueGhostRole(component);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
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

            role.Identifier = _ghostRoleManager.NextIdentifier;
            _ghostRoleManager.QueueGhostRole(role);
        }

        private void OnShutdown(EntityUid uid, GhostRoleComponent role, ComponentShutdown args)
        {
            _ghostRoleManager.RemoveGhostRole(role);
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

    [AnyCommand]
    public sealed class GhostRoleGroupsCommand : IConsoleCommand
    {
        public string Command => "ghostrolegroups";
        public string Description => "Manage ghost role groups.";
        public string Help => @$"${Command}
start <name> <description> <rules>
delete <deleteEntities> <groupIdentifier>
release [groupIdentifier]";

        private void ExecuteStart(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
        {
            if (args.Length < 4)
                return;

            var manager = IoCManager.Resolve<GhostRoleManager>();

            var name = args[1];
            var description = args[2];
            var rules = args[3];

            var id = manager.StartGhostRoleGroup(player, name, description, rules);
            shell.WriteLine($"Role group start: {id}");
        }

        private void ExecuteDelete(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
        {
            var manager = IoCManager.Resolve<GhostRoleManager>();
            if (args.Length != 3)
                return;

            var deleteEntities = bool.Parse(args[1]);
            var identifier = uint.Parse(args[2]);

            manager.DeleteGhostRoleGroup(player, identifier, deleteEntities);
        }

        private void ExecuteRelease(IConsoleShell shell,  IPlayerSession player, string argStr, string[] args)
        {
            var manager = IoCManager.Resolve<GhostRoleManager>();

            switch (args.Length)
            {
                case > 2:
                    shell.WriteLine(Help);
                    break;
                case 2:
                {
                    var identifier = uint.Parse(args[1]);
                    manager.ReleaseGhostRoleGroup(player, identifier);
                    break;
                }
                default:
                {
                    var identifier = manager.GetActiveGhostRoleGroupOrNull(player);
                    if(identifier != null)
                        manager.ReleaseGhostRoleGroup(player, identifier.Value);
                    break;
                }
            }
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteLine("You can only manage ghost role groups on a client.");
                return;
            }

            if (args.Length < 1)
            {
                shell.WriteLine($"Usage: {Help}");
                return;
            }

            var player = (IPlayerSession) shell.Player;

            switch (args[0])
            {
                case "start":
                    ExecuteStart(shell, player, argStr, args);
                    break;
                case "release":
                    ExecuteRelease(shell, player, argStr, args);
                    break;
                case "delete":
                    ExecuteDelete(shell, player, argStr, args);
                    break;
                default:
                    shell.WriteLine($"Usage: {Help}");
                    break;
            }
        }
    }
}
