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

            SubscribeLocalEvent<NodeContainerComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);
            SubscribeLocalEvent<NodeContainerComponent, RotateEvent>(OnRotateEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();


            UnsubscribeLocalEvent<NodeContainerComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);
            UnsubscribeLocalEvent<NodeContainerComponent, RotateEvent>(OnRotateEvent);
        }

        private void OnBodyTypeChanged(EntityUid uid, NodeContainerComponent component, PhysicsBodyTypeChangedEvent args)
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
