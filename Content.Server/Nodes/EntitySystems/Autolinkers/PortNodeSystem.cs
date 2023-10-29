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
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
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
    public void SetConnectable(Entity<PortNodeComponent?> port, bool value)
    {
        if (!Resolve(port, ref port.Comp))
            return;

        if (value == port.Comp.ConnectionsEnabled)
            return;

        port.Comp.ConnectionsEnabled = value;
        _nodeSystem.QueueEdgeUpdate(port);
    }

    /// <summary>
    /// Sets the tag this autoconnector uses to filter ports to connect to.
    /// Also queues an update to the nodes edges so that the change takes effect.
    /// </summary>
    public void SetTag(Entity<PortNodeComponent?> port, string? value)
    {
        if (!Resolve(port, ref port.Comp))
            return;

        if (value == port.Comp.Tag)
            return;

        port.Comp.Tag = value;
        _nodeSystem.QueueEdgeUpdate(port);
    }

    /// <summary>
    /// Sets what edge flags this autoconnector imposes on the edges it creates or enforces.
    /// Also queues an update to the nodes edges so that the change takes effect.
    /// </summary>
    public void SetEdgeFlags(Entity<PortNodeComponent?> port, EdgeFlags value)
    {
        if (!Resolve(port, ref port.Comp))
            return;

        if (value == port.Comp.Flags)
            return;

        port.Comp.Flags = value;
        _nodeSystem.QueueEdgeUpdate(port);
    }


    /// <summary>
    /// Ensures that any edges that would be formed on an edge update continue to exist.
    /// </summary>
    private void OnUpdateEdges(EntityUid uid, PortNodeComponent comp, ref UpdateEdgesEvent args)
    {
        if (!comp.ConnectionsEnabled)
            return;

        if (args.Host.Comp is not { Anchored: true } xform || args.Grid is not { } grid)
            return;

        var tileIndex = _mapSystem.TileIndicesFor(grid.Owner, grid.Comp, xform.Coordinates);
        foreach (var node in _nodeSystem.GetAnchoredNodesOnTile(grid, tileIndex))
        {
            if (node.Owner == uid)
                continue;

            if (comp.Tag is { } tag && !_tagSystem.HasTag(node, tag))
                continue;

            args.Edges ??= new();
            args.Edges[node] = (args.Edges.TryGetValue(node, out var oldFlags) ? oldFlags : EdgeFlags.None) | comp.Flags;
        }
    }

    /// <summary>
    /// Ensures that any edges that would be formed on an edge update continue to exist.
    /// </summary>
    private void OnCheckEdge(EntityUid uid, PortNodeComponent comp, ref CheckEdgeEvent args)
    {
        if (!comp.ConnectionsEnabled)
            return;

        if (args.FromHost.Comp is not { Anchored: true } nodeXform || args.FromGrid is not { } nodeGrid)
            return;

        if (args.ToHost.Comp is not { Anchored: true } edgeXform || args.ToGrid is not { } edgeGrid)
            return;

        if (nodeGrid != edgeGrid)
            return;

        if (_mapSystem.TileIndicesFor(nodeGrid.Owner, nodeGrid.Comp, nodeXform.Coordinates) != _mapSystem.TileIndicesFor(edgeGrid.Owner, edgeGrid.Comp, edgeXform.Coordinates))
            return;

        if (comp.Tag is { } tag && !_tagSystem.HasTag(args.To, tag))
            return;

        args.Wanted = true;
        args.Flags |= comp.Flags;
    }
}
