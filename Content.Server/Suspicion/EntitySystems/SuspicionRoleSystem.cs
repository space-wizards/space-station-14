using System.Collections.Generic;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Suspicion.EntitySystems
{
    [UsedImplicitly]
    public class SuspicionRoleSystem : EntitySystem, IResettingEntitySystem
    {
        private readonly HashSet<SuspicionRoleComponent> _traitors = new();

        public IReadOnlyCollection<SuspicionRoleComponent> Traitors => _traitors;

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

        public void Reset()
        {
            _traitors.Clear();
        }
    }
}
