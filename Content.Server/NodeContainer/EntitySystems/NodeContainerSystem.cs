using Content.Server.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.NodeContainer.EntitySystems
{
    /// <summary>
    ///     Manages <see cref="NodeContainerComponent"/> events.
    /// </summary>
    /// <seealso cref="NodeGroupSystem"/>
    [UsedImplicitly]
    public class NodeContainerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NodeContainerComponent, ComponentInit>(OnInitEvent);
            SubscribeLocalEvent<NodeContainerComponent, ComponentStartup>(OnStartupEvent);
            SubscribeLocalEvent<NodeContainerComponent, ComponentShutdown>(OnShutdownEvent);
            SubscribeLocalEvent<NodeContainerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
            SubscribeLocalEvent<NodeContainerComponent, RotateEvent>(OnRotateEvent);
        }

        private static void OnInitEvent(EntityUid uid, NodeContainerComponent component, ComponentInit args)
        {
            foreach (var (key, node) in component.Nodes)
            {
                node.Name = key;
                node.Initialize(component.Owner);
            }
        }

        private static void OnStartupEvent(EntityUid uid, NodeContainerComponent component, ComponentStartup args)
        {
            foreach (var node in component.Nodes.Values)
            {
                node.OnContainerStartup();
            }
        }

        private static void OnShutdownEvent(EntityUid uid, NodeContainerComponent component, ComponentShutdown args)
        {
            foreach (var node in component.Nodes.Values)
            {
                node.OnContainerShutdown();
            }
        }

        private static void OnAnchorStateChanged(
            EntityUid uid,
            NodeContainerComponent component,
            AnchorStateChangedEvent args)
        {
            foreach (var node in component.Nodes.Values)
            {
                node.AnchorUpdate();
                node.AnchorStateChanged();
            }
        }

        private static void OnRotateEvent(EntityUid uid, NodeContainerComponent container, RotateEvent ev)
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
