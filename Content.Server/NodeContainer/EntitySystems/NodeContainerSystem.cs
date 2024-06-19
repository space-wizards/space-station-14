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
        private EntityQuery<NodeContainerComponent> _query;

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

            _query = GetEntityQuery<NodeContainerComponent>();
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

        public bool TryGetNode<T>(Entity<NodeContainerComponent?> ent, string identifier, [NotNullWhen(true)] out T? node) where T : Node
        {
            if (_query.Resolve(ent, ref ent.Comp, false)
                && ent.Comp.Nodes.TryGetValue(identifier, out var n)
                && n is T t)
            {
                node = t;
                return true;
            }

            node = null;
            return false;
        }

        public bool TryGetNodes<T1, T2>(
            Entity<NodeContainerComponent?> ent,
            string id1,
            string id2,
            [NotNullWhen(true)] out T1? node1,
            [NotNullWhen(true)] out T2? node2)
            where T1 : Node
            where T2 : Node
        {
            if (_query.Resolve(ent, ref ent.Comp, false)
                && ent.Comp.Nodes.TryGetValue(id1, out var n1)
                && n1 is T1 t1
                && ent.Comp.Nodes.TryGetValue(id2, out var n2)
                && n2 is T2 t2)
            {
                node1 = t1;
                node2 = t2;
                return true;
            }

            node1 = null;
            node2 = null;
            return false;
        }

        public bool TryGetNodes<T1, T2, T3>(
            Entity<NodeContainerComponent?> ent,
            string id1,
            string id2,
            string id3,
            [NotNullWhen(true)] out T1? node1,
            [NotNullWhen(true)] out T2? node2,
            [NotNullWhen(true)] out T3? node3)
            where T1 : Node
            where T2 : Node
            where T3 : Node
        {
            if (_query.Resolve(ent, ref ent.Comp, false)
                && ent.Comp.Nodes.TryGetValue(id1, out var n1)
                && n1 is T1 t1
                && ent.Comp.Nodes.TryGetValue(id2, out var n2)
                && n2 is T2 t2
                && ent.Comp.Nodes.TryGetValue(id3, out var n3)
                && n3 is T3 t3)
            {
                node1 = t1;
                node2 = t2;
                node3 = t3;
                return true;
            }

            node1 = null;
            node2 = null;
            node3 = null;
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
