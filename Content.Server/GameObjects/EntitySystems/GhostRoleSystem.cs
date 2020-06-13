using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using Content.Server.GameObjects.Components.Observer;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.EntitySystems
{
    public class GhostRoleSystem : EntitySystem
    {
        private uint _nextRoleIdentifier = 0;
        private Dictionary<uint, GhostRoleComponent> _ghostRoles = new Dictionary<uint, GhostRoleComponent>();

        [ViewVariables]
        public IReadOnlyCollection<GhostRoleComponent> GhostRoles => _ghostRoles.Values;

        public override void Initialize()
        {
            SubscribeNetworkEvent<GhostRoleUpdateRequestMessage>(GhostRolesUpdateRequest);
            SubscribeNetworkEvent<GhostRoleTakeoverRequestMessage>(GhostRoleTakeoverRequest);
        }

        private uint GetNextRoleIdentifier()
        {
            return unchecked(_nextRoleIdentifier++);
        }

        public void RegisterGhostRole(GhostRoleComponent role)
        {
            if(!_ghostRoles.ContainsValue(role))
                _ghostRoles[GetNextRoleIdentifier()] = role;

            RaiseNetworkEvent(new GhostRoleOutdatedMessage());
        }

        public void UnregisterGhostRole(GhostRoleComponent role)
        {
            foreach (var (key, roleComponent) in _ghostRoles)
            {
                if (roleComponent != role) continue;
                _ghostRoles.Remove(key);
                break;
            }

            RaiseNetworkEvent(new GhostRoleOutdatedMessage());
        }

        private void GhostRolesUpdateRequest(GhostRoleUpdateRequestMessage msg, EntitySessionEventArgs args)
        {
            var session = (IPlayerSession)args.SenderSession;

            // TODO: Maybe in the future we shouldn't depend on the player's entity having a GhostComponent.
            if (!(session.AttachedEntity?.HasComponent<GhostComponent>() ?? false))
                return;

            RaiseNetworkEvent(new GhostRoleUpdateMessage(GetGhostRoleInfo()), ((IPlayerSession)args.SenderSession).ConnectedClient);
        }

        private void GhostRoleTakeoverRequest(GhostRoleTakeoverRequestMessage msg, EntitySessionEventArgs args)
        {
            var session = (IPlayerSession)args.SenderSession;

            // TODO: Maybe in the future we shouldn't depend on the player's entity having a GhostComponent.
            if (!(session.AttachedEntity?.HasComponent<GhostComponent>() ?? false))
                return;

            if (!_ghostRoles.TryGetValue(msg.Identifier, out var role) || role.Owner.Deleted)
                return;

            role.Take(session);
        }

        private GhostRoleInfo[] GetGhostRoleInfo()
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
    }
}
