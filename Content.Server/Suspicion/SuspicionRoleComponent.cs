using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Suspicion.Roles;
using Content.Shared.Examine;
using Content.Shared.MobState.Components;
using Content.Shared.Suspicion;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Suspicion
{
    [RegisterComponent]
#pragma warning disable 618
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent, IExamine
#pragma warning restore 618
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

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!IsDead())
            {
                return;
            }

            var traitor = IsTraitor();
            var color = traitor ? "red" : "green";
            var role = traitor ? "suspicion-role-component-role-traitor" : "suspicion-role-component-role-innocent";
            var article = traitor ? "generic-article-a" : "generic-article-an";

            var tooltip = Loc.GetString("suspicion-role-component-on-examine-tooltip",
                                        ("article", Loc.GetString(article)),
                                        ("colorName", color),
                                        ("role",Loc.GetString(role)));

            message.AddMarkup(tooltip);
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
