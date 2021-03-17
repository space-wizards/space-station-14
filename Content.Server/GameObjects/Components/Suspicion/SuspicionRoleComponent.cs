#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems.GameMode;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Suspicion;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent, IExamine
    {
        private Role? _role;
        [ViewVariables]
        private readonly HashSet<SuspicionRoleComponent> _allies = new();

        [ViewVariables]
        public Role? Role
        {
            get => _role;
            set
            {
                if (_role == value)
                {
                    return;
                }

                _role = value;

                Dirty();

                var suspicionRoleSystem = EntitySystem.Get<SuspicionRoleSystem>();

                if (value == null || !value.Antagonist)
                {
                    ClearAllies();
                    suspicionRoleSystem.RemoveTraitor(this);
                }
                else if (value.Antagonist)
                {
                    SetAllies(suspicionRoleSystem.Traitors);
                    suspicionRoleSystem.AddTraitor(this);
                }
            }
        }

        [ViewVariables] public bool KnowsAllies => IsTraitor();

        public bool IsDead()
        {
            return Owner.TryGetComponent(out IMobStateComponent? state) &&
                   state.IsDead();
        }

        public bool IsInnocent()
        {
            return !IsTraitor();
        }

        public bool IsTraitor()
        {
            return Role?.Antagonist ?? false;
        }

        public void SyncRoles()
        {
            if (!Owner.TryGetComponent(out MindComponent? mind) ||
                !mind.HasMind)
            {
                return;
            }

            Role = mind.Mind!.AllRoles.First(role => role is SuspicionRole);
        }

        public void AddAlly(SuspicionRoleComponent ally)
        {
            if (ally == this)
            {
                return;
            }

            _allies.Add(ally);
        }

        public bool RemoveAlly(SuspicionRoleComponent ally)
        {
            if (_allies.Remove(ally))
            {
                Dirty();

                return true;
            }

            return false;
        }

        public void SetAllies(IEnumerable<SuspicionRoleComponent> allies)
        {
            _allies.Clear();

            _allies.UnionWith(allies.Where(a => a != this));

            Dirty();
        }

        public void ClearAllies()
        {
            _allies.Clear();

            Dirty();
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!IsDead())
            {
                return;
            }

            var traitor = IsTraitor();
            var color = traitor ? "red" : "green";
            var role = traitor ? "traitor" : "innocent";
            var article = traitor ? "a" : "an";

            var tooltip = Loc.GetString("They were {0} [color={1}]{2}[/color]!", Loc.GetString(article), color,
                Loc.GetString(role));

            message.AddMarkup(tooltip);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            if (Role == null)
            {
                return new SuspicionRoleComponentState(null, null, Array.Empty<(string, EntityUid)>());
            }

            var allies = new List<(string name, EntityUid)>();

            foreach (var role in _allies)
            {
                if (role.Role?.Mind.CharacterName == null)
                {
                    continue;
                }

                allies.Add((role.Role!.Mind.CharacterName, role.Owner.Uid));
            }

            return new SuspicionRoleComponentState(Role?.Name, Role?.Antagonist, allies.ToArray());
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg:
                case PlayerDetachedMsg:
                    SyncRoles();
                    break;
                case RoleAddedMessage {Role: SuspicionRole role}:
                    Role = role;
                    break;
                case RoleRemovedMessage {Role: SuspicionRole}:
                    Role = null;
                    break;
            }
        }
    }
}
