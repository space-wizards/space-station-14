using System.Linq;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class NodeContainerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RotateEvent>(RotateEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<RotateEvent>();
        }

        private void RotateEvent(RotateEvent ev)
        {
            if (!ev.Sender.TryGetComponent(out NodeContainerComponent container))
            {
                return;
            }

            if (ev.NewRotation == ev.OldRotation)
            {
                return;
            }

            foreach (var rotatableNode in container.Nodes.OfType<IRotatableNode>())
            {
                rotatableNode.RotateEvent(ev);
            }
        }
    }
}
