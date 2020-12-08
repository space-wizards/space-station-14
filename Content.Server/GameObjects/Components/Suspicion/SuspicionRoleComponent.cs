#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Suspicion;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent, IExamine
    {
        private Role? _role;
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
            return Owner.TryGetComponent(out MindComponent? mind) &&
                   mind.HasMind &&
                   mind.Mind!.HasRole<SuspicionInnocentRole>();
        }

        public bool IsTraitor()
        {
            return Owner.TryGetComponent(out MindComponent? mind) &&
                   mind.HasMind &&
                   mind.Mind!.HasRole<SuspicionTraitorRole>();
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

            if (KnowsAllies && Owner.TryGetComponent(out IActorComponent? actor))
            {
                var channel = actor.playerSession.ConnectedClient;
                DebugTools.AssertNotNull(channel);

                var message = new SuspicionAllyAddedMessage(ally.Owner.Uid);

                SendNetworkMessage(message, channel);
            }
        }

        public bool RemoveAlly(SuspicionRoleComponent ally)
        {
            if (ally == this)
            {
                return false;
            }

            if (_allies.Remove(ally))
            {
                if (KnowsAllies && Owner.TryGetComponent(out IActorComponent? actor))
                {
                    var channel = actor.playerSession.ConnectedClient;
                    DebugTools.AssertNotNull(channel);

                    var message = new SuspicionAllyRemovedMessage(ally.Owner.Uid);

                    SendNetworkMessage(message, channel);
                }

                return true;
            }

            return false;
        }

        public void SetAllies(IEnumerable<SuspicionRoleComponent> allies)
        {
            _allies.Clear();

            foreach (var ally in allies)
            {
                if (ally == this)
                {
                    continue;
                }

                _allies.Add(ally);
            }

            if (!KnowsAllies ||
                !Owner.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var channel = actor.playerSession.ConnectedClient;
            DebugTools.AssertNotNull(channel);

            var message = new SuspicionAlliesMessage(_allies.Select(role => role.Owner.Uid));

            SendNetworkMessage(message, channel);
        }

        public void ClearAllies()
        {
            _allies.Clear();

            if (!KnowsAllies ||
                !Owner.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var channel = actor.playerSession.ConnectedClient;
            DebugTools.AssertNotNull(channel);

            var message = new SuspicionAlliesClearedMessage();

            SendNetworkMessage(message, channel);
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

        public override void OnRemove()
        {
            Role = null;
            base.OnRemove();
        }

        public override ComponentState GetComponentState()
        {
            return Role == null
                ? new SuspicionRoleComponentState(null, null)
                : new SuspicionRoleComponentState(Role?.Name, Role?.Antagonist);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (message is not RoleMessage msg ||
                msg.Role is not SuspicionRole role)
            {
                return;
            }

            switch (message)
            {
                case PlayerAttachedMsg _:
                case PlayerDetachedMsg _:
                    SyncRoles();
                    break;
                case RoleAddedMessage _:
                    Role = role;
                    break;
                case RoleRemovedMessage _:
                    Role = null;
                    break;
            }
        }
    }
}
