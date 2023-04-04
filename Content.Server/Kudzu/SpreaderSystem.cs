using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Kudzu;

public sealed class SpreaderNode : Node
{
    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform, EntityQuery<NodeContainerComponent> nodeQuery, EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid, IEntityManager entMan)
    {
        if (grid == null)
            yield break;

        var pos = grid.TileIndicesFor(xform.Coordinates);

        for (var i = 0; i < 4; i++)
        {
            var dir = (Direction) (2 * i);
            var neighbor = pos + dir.ToIntVec();

            if (!grid.TryGetTileRef(neighbor, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            var anchored = grid.GetAnchoredEntitiesEnumerator(neighbor);

            while (anchored.MoveNext(out var ent))
            {
                if (!nodeQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                if (nodeContainer.Nodes.TryGetValue("puddle", out var neighborNode))
                {
                    yield return neighborNode;
                }
            }
        }
    }
}

[RegisterComponent]
public sealed class EdgeSpreaderComponent : Component
{

}

[NodeGroup(NodeGroupID.Spreader)]
public sealed class SpreaderNodeGroup : BaseNodeGroup
{
    private IEntityManager _entManager = default!;

    public readonly HashSet<EntityUid> Edges = new();

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);
        _entManager = entMan;
    }

    private bool IsEdge(Node node, EntityQuery<NodeContainerComponent> containerQuery, EntityQuery<TransformComponent> xformQuery, Node? ignoredNode = null)
    {
        var xform = xformQuery.GetComponent(node.Owner);

        if (!_entManager.TryGetComponent<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var pos = grid.TileIndicesFor(xform.Coordinates);

        for (var i = 0; i < 4; i++)
        {
            var dir = (Direction) (2 * i);
            var neighbor = pos + dir.ToIntVec();

            if (!grid.TryGetTileRef(neighbor, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            var anchored = grid.GetAnchoredEntitiesEnumerator(neighbor);
            var isEdge = true;

            while (anchored.MoveNext(out var ent))
            {
                if (!containerQuery.TryGetComponent(ent, out var nodeContainer))
                    continue;

                if (nodeContainer.Nodes.TryGetValue("puddle", out var neighborNode) && neighborNode != ignoredNode)
                {
                    isEdge = false;
                    break;
                }
            }

            if (isEdge)
                return true;
        }

        return false;
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);
        Edges.Remove(node.Owner);

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var containerQuery = _entManager.GetEntityQuery<NodeContainerComponent>();

        // Iterate neighbors and check if they need to be made spreaders.
        foreach (var neighborNode in node.ReachableNodes)
        {
            if (!IsEdge(neighborNode, containerQuery, xformQuery, node))
                continue;

            _entManager.EnsureComponent<EdgeSpreaderComponent>(neighborNode.Owner);
            Edges.Add(node.Owner);
        }
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        var containerQuery = _entManager.GetEntityQuery<NodeContainerComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var node in groupNodes)
        {
            if (!IsEdge(node, containerQuery, xformQuery))
                continue;

            // Cleanup isn't super important as worst case we just iterate and realise the
            // neighbors are no longer valid edges.
            _entManager.EnsureComponent<EdgeSpreaderComponent>(node.Owner);
            Edges.Add(node.Owner);
        }
    }
}

public sealed class SpreaderSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly NodeGroupSystem _groups = default!;

    // TODO: add spreaders into node groups.
    // Rate-limit spreading per node-group.
    // Copy some fluid stuff across.
    // Copy smokes across.

    public override void Initialize()
    {
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<SpreaderComponent, ComponentStartup>(OnSpreaderStartup);
    }

    private void OnSpreaderStartup(EntityUid uid, SpreaderComponent component, ComponentStartup args)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer))
            return;


    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        UpdateNearbySpreaders(ev.Entity, ev.Airtight);
    }

    public void UpdateNearbySpreaders(EntityUid blocker, AirtightComponent comp)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(blocker, out var transform))
            return; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid)) return;

        var spreaderQuery = GetEntityQuery<SpreaderComponent>();
        var tile = grid.TileIndicesFor(transform.Coordinates);

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction)) continue;

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (spreaderQuery.TryGetComponent(ent, out var s) && s.Enabled)
                {
                    // _edgeGrowths.Add(ent.Value);
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EdgeSpreaderComponent>();
        // TODO: Per-group spread rates and stuffsies
        // Events and stuff

        while (query.MoveNext(out var uid, out var comp))
        {

        }
    }

    private bool TryGrow(EntityUid ent, TransformComponent? transform = null, SpreaderComponent? spreader = null)
    {
        if (!Resolve(ent, ref transform, ref spreader, false))
            return false;

        if (spreader.Enabled == false) return false;

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid)) return false;

        var didGrow = false;

        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag) (1 << i);
            var coords = transform.Coordinates.Offset(direction.AsDir().ToVec());
            if (grid.GetTileRef(coords).Tile.IsEmpty || _robustRandom.Prob(1 - spreader.Chance)) continue;
            var ents = grid.GetLocal(coords);

            if (ents.Any(x => IsTileBlockedFrom(x, direction))) continue;

            // Ok, spawn a plant
            didGrow = true;
            EntityManager.SpawnEntity(spreader.GrowthResult, transform.Coordinates.Offset(direction.AsDir().ToVec()));
        }

        return didGrow;
    }

    private bool IsTileBlockedFrom(EntityUid ent, DirectionFlag dir)
    {
        if (EntityManager.TryGetComponent<SpreaderComponent>(ent, out _))
            return true;

        if (!EntityManager.TryGetComponent<AirtightComponent>(ent, out var airtight))
            return false;

        var oppositeDir = dir.AsDir().GetOpposite().ToAtmosDirection();

        return airtight.AirBlocked && airtight.AirBlockedDirection.IsFlagSet(oppositeDir);
    }
}
