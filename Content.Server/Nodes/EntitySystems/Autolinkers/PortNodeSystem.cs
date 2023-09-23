using Content.Server.Nodes.Components.Autolinkers;
using Content.Server.Nodes.Events;
using Content.Shared.Nodes;
using Content.Shared.Tag;

namespace Content.Server.Nodes.EntitySystems.Autolinkers;


/// <summary>
/// The system responsible for creating automatic links between graph nodes located on the same tile.
/// The corresponding component is <see cref="PortNodeComponent"/>.
/// </summary>
public sealed partial class PortNodeSystem : EntitySystem
{
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PortNodeComponent, UpdateEdgesEvent>(OnUpdateEdges);
        SubscribeLocalEvent<PortNodeComponent, CheckEdgeEvent>(OnCheckEdge);
    }


    /// <summary>
    /// Sets whether this port can create and maintain links to other nodes.
    /// Also queues an update to the nodes edges so that the change takes effect.
    /// </summary>
    public void SetConnectable(EntityUid uid, bool value, PortNodeComponent? port = null)
    {
        if (!Resolve(uid, ref port))
            return;

        if (value == port.ConnectionsEnabled)
            return;

        port.ConnectionsEnabled = value;
        _nodeSystem.QueueEdgeUpdate(uid);
    }

    /// <summary>
    /// Sets the tag this autoconnector uses to filter ports to connect to.
    /// Also queues an update to the nodes edges so that the change takes effect.
    /// </summary>
    public void SetTag(EntityUid uid, string? value, PortNodeComponent? port = null)
    {
        if (!Resolve(uid, ref port))
            return;

        if (value == port.Tag)
            return;

        port.Tag = value;
        _nodeSystem.QueueEdgeUpdate(uid);
    }

    /// <summary>
    /// Sets what edge flags this autoconnector imposes on the edges it creates or enforces.
    /// Also queues an update to the nodes edges so that the change takes effect.
    /// </summary>
    public void SetEdgeFlags(EntityUid uid, EdgeFlags value, PortNodeComponent? port = null)
    {
        if (!Resolve(uid, ref port))
            return;

        if (value == port.Flags)
            return;

        port.Flags = value;
        _nodeSystem.QueueEdgeUpdate(uid);
    }


    /// <summary>
    /// Ensures that any edges that would be formed on an edge update continue to exist.
    /// </summary>
    private void OnUpdateEdges(EntityUid uid, PortNodeComponent comp, ref UpdateEdgesEvent args)
    {
        if (!comp.ConnectionsEnabled)
            return;

        var xform = args.HostXform;
        if (!xform.Anchored || args.HostGrid is not { } grid)
            return;

        var tileIndex = grid.TileIndicesFor(xform.Coordinates);
        foreach (var nodeId in _nodeSystem.GetAnchoredNodesOnTile(grid, tileIndex))
        {
            if (nodeId == uid)
                continue;

            if (comp.Tag is { } tag && !_tagSystem.HasTag(nodeId, tag))
                continue;

            args.Edges ??= new();
            args.Edges[nodeId] = (args.Edges.TryGetValue(nodeId, out var oldFlags) ? oldFlags : EdgeFlags.None) | comp.Flags;
        }
    }

    /// <summary>
    /// Ensures that any edges that would be formed on an edge update continue to exist.
    /// </summary>
    private void OnCheckEdge(EntityUid uid, PortNodeComponent comp, ref CheckEdgeEvent args)
    {
        if (!comp.ConnectionsEnabled)
            return;

        var nodeXform = args.NodeHostXform;
        if (!nodeXform.Anchored || args.NodeHostGrid is not { } nodeGrid)
            return;

        var edgeXform = args.EdgeHostXform;
        if (!edgeXform.Anchored || args.EdgeHostGrid is not { } edgeGrid)
            return;

        if (!ReferenceEquals(nodeGrid, edgeGrid))
            return;

        if (nodeGrid.TileIndicesFor(nodeXform.Coordinates) != edgeGrid.TileIndicesFor(edgeXform.Coordinates))
            return;

        if (comp.Tag is { } tag && !_tagSystem.HasTag(args.EdgeId, tag))
            return;

        args.Wanted = true;
        args.Flags |= comp.Flags;
    }
}
