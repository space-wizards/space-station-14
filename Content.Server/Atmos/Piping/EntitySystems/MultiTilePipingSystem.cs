using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed partial class MultiTilePipingSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private NodeContainerSystem _nodes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MultiTilePipingComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<MultiTilePipingComponent, AnchorStateChangedEvent>(OnAnchorStateChangedEvent);
        SubscribeLocalEvent<MultiTilePipingComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<MultiTilePipingComponent, ComponentShutdown>(OnShutdownEvent);
    }

    private void OnMapInitEvent(Entity<MultiTilePipingComponent> ent, ref MapInitEvent args) => UpdatePorts(ent);

    private void OnAnchorStateChangedEvent(Entity<MultiTilePipingComponent> ent, ref AnchorStateChangedEvent args) => UpdatePorts(ent);

    private void OnMoveEvent(Entity<MultiTilePipingComponent> ent, ref MoveEvent args) => UpdatePorts(ent);

    private void UpdatePorts(Entity<MultiTilePipingComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || LifeStage(ent) < EntityLifeStage.MapInitialized)
            return;

        var xform = Transform(ent);
        if (!xform.Anchored || xform.GridUid == null)
        {
            RemovePorts(ent);
            return;
        }

        foreach (var (nodeName, port) in ent.Comp.Ports)
        {
            if (_nodes.TryGetNode<PipeNode>(ent.Owner, nodeName, out var internalNode))
                UpdatePort(ent, nodeName, port, internalNode, xform);
        }
    }

    private void UpdatePort(Entity<MultiTilePipingComponent> ent, string nodeName, MultiTilePipePort port,
        PipeNode internalNode, TransformComponent xform)
    {
        var coordinates = new EntityCoordinates(xform.ParentUid, xform.LocalPosition + xform.LocalRotation.RotateVec(port.Offset));
        var rotation = xform.LocalRotation + port.Rotation;
        if (!ent.Comp.PortEntities.TryGetValue(nodeName, out var uid) || TerminatingOrDeleted(uid))
        {
            uid = Spawn(port.Prototype, coordinates);
            ent.Comp.PortEntities[nodeName] = uid;
            _transform.SetLocalRotation(uid, rotation);
        }
        else
        {
            _transform.SetCoordinates((uid, Transform(uid), MetaData(uid)), coordinates, rotation);
            _transform.AnchorEntity(uid);
        }

        if (!_nodes.TryGetNode<PipeNode>(uid, port.Node, out var terminal))
            return;

        internalNode.AddAlwaysReachable(terminal);
        terminal.AddAlwaysReachable(internalNode);
    }

    private void OnShutdownEvent(Entity<MultiTilePipingComponent> ent, ref ComponentShutdown args) => RemovePorts(ent);

    private void RemovePorts(Entity<MultiTilePipingComponent> ent)
    {
        foreach (var (nodeName, uid) in ent.Comp.PortEntities)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (ent.Comp.Ports.TryGetValue(nodeName, out var port) &&
                _nodes.TryGetNode<PipeNode>(ent.Owner, nodeName, out var internalNode) &&
                _nodes.TryGetNode<PipeNode>(uid, port.Node, out var terminal))
            {
                internalNode.RemoveAlwaysReachable(terminal);
                terminal.RemoveAlwaysReachable(internalNode);
            }
            QueueDel(uid);
        }
        ent.Comp.PortEntities.Clear();
    }
}
