using Content.Server.NodeContainer.Nodes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.NodeContainer.EntitySystems
{
    /// <summary>
    ///     Manages <see cref="NodeContainerComponent"/> events.
    /// </summary>
    /// <seealso cref="NodeGroupSystem"/>
    [UsedImplicitly]
    public sealed class NodeContainerSystem : EntitySystem
    {
        [Dependency] private readonly NodeGroupSystem _nodeGroupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NodeContainerComponent, ComponentInit>(OnInitEvent);
            SubscribeLocalEvent<NodeContainerComponent, ComponentStartup>(OnStartupEvent);
            SubscribeLocalEvent<NodeContainerComponent, ComponentShutdown>(OnShutdownEvent);
            SubscribeLocalEvent<NodeContainerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
            SubscribeLocalEvent<NodeContainerComponent, RotateEvent>(OnRotateEvent);
        }

        private void OnInitEvent(EntityUid uid, NodeContainerComponent component, ComponentInit args)
        {
            foreach (var (key, node) in component.Nodes)
            {
                node.Name = key;
                node.Initialize(component.Owner, EntityManager);
            }
        }

        private void OnStartupEvent(EntityUid uid, NodeContainerComponent component, ComponentStartup args)
        {
            foreach (var node in component.Nodes.Values)
            {
                _nodeGroupSystem.QueueReflood(node);
            }
        }

        private void OnShutdownEvent(EntityUid uid, NodeContainerComponent component, ComponentShutdown args)
        {
            foreach (var node in component.Nodes.Values)
            {
                _nodeGroupSystem.QueueNodeRemove(node);
                node.Deleting = true;
            }
        }

        private void OnAnchorStateChanged(
            EntityUid uid,
            NodeContainerComponent component,
            ref AnchorStateChangedEvent args)
        {
            foreach (var node in component.Nodes.Values)
            {
                if (!node.NeedAnchored)
                    continue;

                if (args.Anchored)
                    _nodeGroupSystem.QueueReflood(node);
                else
                    _nodeGroupSystem.QueueNodeRemove(node);
            }
        }

        private void OnRotateEvent(EntityUid uid, NodeContainerComponent container, ref RotateEvent ev)
        {
            if (ev.NewRotation == ev.OldRotation)
            {
                return;
            }

            var anchored = Transform(uid).Anchored;

            foreach (var node in container.Nodes.Values)
            {
                if (node.NeedAnchored && !anchored)
                    continue;

                if (node is not IRotatableNode rotatableNode)
                    continue;

                if (rotatableNode.RotateEvent(ref ev))
                    _nodeGroupSystem.QueueReflood(node);
            }
        }
    }
}
