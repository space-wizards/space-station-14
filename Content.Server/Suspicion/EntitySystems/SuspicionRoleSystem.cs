using System.Collections.Generic;
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
            SubscribeLocalEvent<SuspicionRoleComponent, PlayerAttachedEvent>((HandlePlayerAttached));
            SubscribeLocalEvent<SuspicionRoleComponent, PlayerDetachedEvent>((HandlePlayerDetached));
        }

        private void HandlePlayerDetached(EntityUid uid, SuspicionRoleComponent component, PlayerDetachedEvent args)
        {
            component.SyncRoles();
        }

        private void HandlePlayerAttached(EntityUid uid, SuspicionRoleComponent component, PlayerAttachedEvent args)
        {
            component.SyncRoles();
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
