using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Suspicion;
using Content.Shared.GameObjects.Components.Suspicion;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SuspicionRoleSystem : EntitySystem
    {
        private readonly HashSet<SuspicionRoleComponent> _antagonists = new HashSet<SuspicionRoleComponent>();

        public void AddAntagonist(SuspicionRoleComponent role)
        {
            _antagonists.Add(role);
        }

        public void RemoveAntagonist(SuspicionRoleComponent role)
        {
            _antagonists.Remove(role);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _antagonists.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_antagonists.Count == 0)
            {
                return;
            }

            var announceTo = new List<INetChannel>();

            foreach (var role in ComponentManager.EntityQuery<SuspicionRoleComponent>())
            {
                if (!role.AnnounceTraitors)
                {
                    continue;
                }

                role.AnnounceTraitors = false;

                if (!role.Owner.TryGetComponent(out IActorComponent actor))
                {
                    continue;
                }

                announceTo.Add(actor.playerSession.ConnectedClient);
            }

            if (announceTo.Count == 0)
            {
                return;
            }

            var antagonistIds = _antagonists.Select(t => t.Owner.Name).ToHashSet();
            var message = new SuspicionAlliesMessage(antagonistIds);

            foreach (var channel in announceTo)
            foreach (var traitor in _antagonists)
            {
                EntityNetworkManager.SendComponentNetworkMessage(channel, traitor.Owner, traitor, message);
            }
        }
    }
}
