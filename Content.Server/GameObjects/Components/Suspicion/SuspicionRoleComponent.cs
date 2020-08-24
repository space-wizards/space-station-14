#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Suspicion;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent, IExamine
    {
        private Role? _role;

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

                if (value == null)
                {
                    suspicionRoleSystem.RemoveAntagonist(this);
                }
                else if (value.Antagonist)
                {
                    AnnounceTraitors = true;
                    suspicionRoleSystem.AddAntagonist(this);
                }
                else
                {
                    AnnounceTraitors = false;
                    suspicionRoleSystem.RemoveAntagonist(this);
                }
            }
        }

        public bool AnnounceTraitors { get; set; }

        public bool IsDead()
        {
            return Owner.TryGetComponent(out IDamageableComponent? damageable) &&
                   damageable.CurrentDamageState == DamageState.Dead;
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

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!IsDead())
            {
                return;
            }

            var tooltip = IsTraitor()
                ? Loc.GetString($"They were a [color=red]traitor[/color]!")
                : Loc.GetString($"They were an [color=green]innocent[/color]!");

            message.AddMarkup(tooltip);
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

            if (!(message is RoleMessage msg) ||
                !(msg.Role is SuspicionRole role))
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
