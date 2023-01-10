using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Suspicion.Roles;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Suspicion;

namespace Content.Server.Suspicion
{
    [RegisterComponent]
    public sealed class SuspicionRoleComponent : SharedSuspicionRoleComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
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

                var sus = EntitySystem.Get<SuspicionRuleSystem>();

                if (value == null || !value.Antagonist)
                {
                    ClearAllies();
                    sus.RemoveTraitor(this);
                }
                else if (value.Antagonist)
                {
                    SetAllies(sus.Traitors);
                    sus.AddTraitor(this);
                }
            }
        }

        [ViewVariables] public bool KnowsAllies => IsTraitor();

        public bool IsDead()
        {
            return _entMan.TryGetComponent(Owner, out MobStateComponent? state) &&
                   _entMan.EntitySysManager.GetEntitySystem<MobStateSystem>().IsDead(Owner, state);
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
            if (!_entMan.TryGetComponent(Owner, out MindComponent? mind) ||
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
        public override ComponentState GetComponentState()
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

                allies.Add((role.Role!.Mind.CharacterName, role.Owner));
            }

            return new SuspicionRoleComponentState(Role?.Name, Role?.Antagonist, allies.ToArray());
        }
    }
}
