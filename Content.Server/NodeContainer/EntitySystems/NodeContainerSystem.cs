using Content.Server.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.NodeContainer.EntitySystems
{
    [UsedImplicitly]
    public class NodeContainerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NodeContainerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
            SubscribeLocalEvent<NodeContainerComponent, RotateEvent>(OnRotateEvent);
        }

        private void OnAnchorStateChanged(EntityUid uid, NodeContainerComponent component, AnchorStateChangedEvent args)
        {
            component.AnchorUpdate();
        }

        private void OnRotateEvent(EntityUid uid, NodeContainerComponent container, RotateEvent ev)
        {
            if (ev.NewRotation == ev.OldRotation)
            {
                return;
            }

            foreach (var node in container.Nodes.Values)
            {
                if (node is not IRotatableNode rotatableNode) continue;
                rotatableNode.RotateEvent(ev);
            }
        }
    }
}
