using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.Observer.GhostRoles;
using Content.Shared.GameObjects.Components.Observer.GhostRoles;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GhostRoleSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;

        private uint _nextRoleIdentifier = 0;
        private readonly Dictionary<uint, GhostRoleComponent> _ghostRoles = new();
        private readonly Dictionary<IPlayerSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        [ViewVariables]
        public IReadOnlyCollection<GhostRoleComponent> GhostRoles => _ghostRoles.Values;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerAttachSystemMessage>(OnPlayerAttached);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<PlayerAttachSystemMessage>();
        }

        private uint GetNextRoleIdentifier()
        {
            return unchecked(_nextRoleIdentifier++);
        }

        public void OpenEui(IPlayerSession session)
        {
            if (session.AttachedEntity == null || !session.AttachedEntity.HasComponent<GhostComponent>())
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
                eui?.Close();
            }
        }

        public void UpdateAllEui()
        {
            foreach (var eui in _openUis.Values)
            {
                eui.StateDirty();
            }
        }

        public void RegisterGhostRole(GhostRoleComponent role)
        {
            if (_ghostRoles.ContainsValue(role)) return;
            _ghostRoles[role.Identifier = GetNextRoleIdentifier()] = role;
            UpdateAllEui();

        }

        public void UnregisterGhostRole(GhostRoleComponent role)
        {
            if (!_ghostRoles.ContainsKey(role.Identifier) || _ghostRoles[role.Identifier] != role) return;
            _ghostRoles.Remove(role.Identifier);
            UpdateAllEui();
        }

        public void Takeover(IPlayerSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role)) return;
            if (!role.Take(player)) return;
            CloseEui(player);
        }

        public GhostRoleInfo[] GetGhostRolesInfo()
        {
            var roles = new GhostRoleInfo[_ghostRoles.Count];

            var i = 0;

            foreach (var (id, role) in _ghostRoles)
            {
                roles[i] = new GhostRoleInfo(){Identifier = id, Name = role.RoleName, Description = role.RoleDescription};
                i++;
            }

            return roles;
        }

        private void OnPlayerAttached(PlayerAttachSystemMessage message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.NewPlayer)) return;
            if (message.Entity.HasComponent<GhostComponent>()) return;
            CloseEui(message.NewPlayer);
        }

        public void Reset()
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
            _ghostRoles.Clear();
            _nextRoleIdentifier = 0;
        }
    }

    [AnyCommand]
    public class GhostRoles : IConsoleCommand
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
