using System.Collections.Generic;
using Content.Server.Roles;
using Content.Server.Suspicion.Roles;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Suspicion.EntitySystems
{
    [UsedImplicitly]
    public class SuspicionRoleSystem : EntitySystem
    {
        private readonly HashSet<SuspicionRoleComponent> _traitors = new();

        public IReadOnlyCollection<SuspicionRoleComponent> Traitors => _traitors;

        #region Overrides of EntitySystem

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<SuspicionRoleComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<SuspicionRoleComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<SuspicionRoleComponent, RoleAddedEvent>(OnRoleAdded);
            SubscribeLocalEvent<SuspicionRoleComponent, RoleRemovedEvent>(OnRoleRemoved);
        }

        private void OnPlayerDetached(EntityUid uid, SuspicionRoleComponent component, PlayerDetachedEvent args)
        {
            component.SyncRoles();
        }

        private void OnPlayerAttached(EntityUid uid, SuspicionRoleComponent component, PlayerAttachedEvent args)
        {
            component.SyncRoles();
        }

        private void OnRoleAdded(EntityUid uid, SuspicionRoleComponent component, RoleAddedEvent args)
        {
            if (args.Role is not SuspicionRole role) return;
            component.Role = role;
        }

        private void OnRoleRemoved(EntityUid uid, SuspicionRoleComponent component, RoleRemovedEvent args)
        {
            if (args.Role is not SuspicionRole) return;
            component.Role = null;
        }

        #endregion

        public void AddTraitor(SuspicionRoleComponent role)
        {
            if (!_traitors.Add(role))
            {
                return;
            }

            foreach (var traitor in _traitors)
            {
                traitor.AddAlly(role);
            }

            role.SetAllies(_traitors);
        }

        public void RemoveTraitor(SuspicionRoleComponent role)
        {
            if (!_traitors.Remove(role))
            {
                return;
            }

            foreach (var traitor in _traitors)
            {
                traitor.RemoveAlly(role);
            }

            role.ClearAllies();
        }

        public override void Shutdown()
        {
            _traitors.Clear();
            base.Shutdown();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _traitors.Clear();
        }
    }
}
