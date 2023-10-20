using System.Diagnostics.CodeAnalysis;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Examine;
using JetBrains.Annotations;

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
            SubscribeLocalEvent<NodeContainerComponent, ReAnchorEvent>(OnReAnchor);
            SubscribeLocalEvent<NodeContainerComponent, MoveEvent>(OnMoveEvent);
            SubscribeLocalEvent<NodeContainerComponent, ExaminedEvent>(OnExamine);
        }

        public bool TryGetNode<T>(NodeContainerComponent component, string? identifier, [NotNullWhen(true)] out T? node) where T : Node
        {
            if (identifier == null)
            {
                node = null;
                return false;
            }

            if (component.Nodes.TryGetValue(identifier, out var n) && n is T t)
            {
                node = t;
                return true;
            }

            node = null;
            return false;
        }

        private void OnInitEvent(EntityUid uid, NodeContainerComponent component, ComponentInit args)
        {
            foreach (var (key, node) in component.Nodes)
            {
                node.Name = key;
                node.Initialize(uid, EntityManager);
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

                node.OnAnchorStateChanged(EntityManager, args.Anchored);

                if (args.Anchored)
                    _nodeGroupSystem.QueueReflood(node);
                else
                    _nodeGroupSystem.QueueNodeRemove(node);
            }
        }

        private void OnReAnchor(EntityUid uid, NodeContainerComponent component, ref ReAnchorEvent args)
        {
            foreach (var node in component.Nodes.Values)
            {
                _nodeGroupSystem.QueueNodeRemove(node);
                _nodeGroupSystem.QueueReflood(node);
            }
        }

        private void OnMoveEvent(EntityUid uid, NodeContainerComponent container, ref MoveEvent ev)
        {
            if (ev.NewRotation == ev.OldRotation)
            {
                return;
            }

            var xform = ev.Component;

            foreach (var node in container.Nodes.Values)
            {
                if (node is not IRotatableNode rotatableNode)
                    continue;

                // Don't bother updating nodes that can't even be connected to anything atm.
                if (!node.Connectable(EntityManager, xform))
                    continue;

                if (rotatableNode.RotateNode(in ev))
                    _nodeGroupSystem.QueueReflood(node);
            }
        }

        private void OnExamine(EntityUid uid, NodeContainerComponent component, ExaminedEvent args)
        {
            if (!component.Examinable || !args.IsInDetailsRange)
                return;

            foreach (var node in component.Nodes.Values)
            {
                if (node == null) continue;
                switch (node.NodeGroupID)
                {
                    case NodeGroupID.HVPower:
                        args.PushMarkup(
                            Loc.GetString("node-container-component-on-examine-details-hvpower"));
                        break;
                    case NodeGroupID.MVPower:
                        args.PushMarkup(
                            Loc.GetString("node-container-component-on-examine-details-mvpower"));
                        break;
                    case NodeGroupID.Apc:
                        args.PushMarkup(
                            Loc.GetString("node-container-component-on-examine-details-apc"));
                        break;
                }
            }
        }
    }
}
