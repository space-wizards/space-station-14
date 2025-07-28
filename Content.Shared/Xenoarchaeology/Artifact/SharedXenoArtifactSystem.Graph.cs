using System.Diagnostics.CodeAnalysis;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Xenoarchaeology.Artifact;

/// <summary>
/// User-friendly API for viewing and modifying the complex graph relationship in XenoArtifacts
/// </summary>
public abstract partial class SharedXenoArtifactSystem
{
    /// <summary>
    /// Gets the index, corresponding to a given node, throwing if the node is not present.
    /// </summary>
    public int GetIndex(Entity<XenoArtifactComponent> ent, EntityUid node)
    {
        if (TryGetIndex((ent, ent), node, out var index))
        {
            return index.Value;
        }

        throw new ArgumentException($"node {ToPrettyString(node)} is not present in {ToPrettyString(ent)}");
    }

    /// <summary>
    /// Tries to get index inside nodes collection, corresponding to a given node EntityUid.
    /// </summary>
    public bool TryGetIndex(Entity<XenoArtifactComponent?> ent, EntityUid node, [NotNullWhen(true)] out int? index)
    {
        index = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        for (var i = 0; i < ent.Comp.NodeVertices.Length; i++)
        {
            if (!TryGetNode(ent, i, out var iNode))
                continue;

            if (node != iNode.Value.Owner)
                continue;

            index = i;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets node entity with node component from artifact by index of node inside artifact nodes collection.
    /// </summary>
    /// <exception cref="ArgumentException">Throws if requested index doesn't exist on artifact. </exception>
    public Entity<XenoArtifactNodeComponent> GetNode(Entity<XenoArtifactComponent> ent, int index)
    {
        if (ent.Comp.NodeVertices[index] is { } netUid && GetEntity(netUid) is var uid)
            return (uid, XenoArtifactNode(uid));

        throw new ArgumentException($"index {index} does not correspond to an existing node in {ToPrettyString(ent)}");
    }

    /// <summary>
    /// Tries to get node entity with node component from artifact by index of node inside artifact nodes collection.
    /// </summary>
    public bool TryGetNode(Entity<XenoArtifactComponent?> ent, int index, [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node)
    {
        node = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (index < 0 || index >= ent.Comp.NodeVertices.Length)
            return false;

        if (ent.Comp.NodeVertices[index] is { } netUid && GetEntity(netUid) is var uid)
            node = (uid, XenoArtifactNode(uid));

        return node != null;
    }

    /// <summary>
    /// Gets the index of the first empty spot in the NodeVertices array.
    /// If there is none, resizes both arrays and returns the new index.
    /// </summary>
    public int GetFreeNodeIndex(Entity<XenoArtifactComponent> ent)
    {
        var length = ent.Comp.NodeVertices.Length;
        for (var i = 0; i < length; i++)
        {
            if (ent.Comp.NodeVertices[i] == null)
                return i;
        }

        ResizeNodeGraph(ent, length + 1);
        return length;
    }

    /// <summary>
    /// Extracts node entities from artifact container
    /// (uses pre-cached <see cref="XenoArtifactComponent.NodeVertices"/> and mapping from NetEntity).
    /// </summary>
    public IEnumerable<Entity<XenoArtifactNodeComponent>> GetAllNodes(Entity<XenoArtifactComponent> ent)
    {
        foreach (var netNode in ent.Comp.NodeVertices)
        {
            if (TryGetEntity(netNode, out var node))
                yield return (node.Value, XenoArtifactNode(node.Value));
        }
    }

    /// <summary>
    /// Extracts enumeration of all indices that artifact node container have.
    /// </summary>
    public IEnumerable<int> GetAllNodeIndices(Entity<XenoArtifactComponent> ent)
    {
        for (var i = 0; i < ent.Comp.NodeVertices.Length; i++)
        {
            if (ent.Comp.NodeVertices[i] is not null)
                yield return i;
        }
    }

    /// <summary>
    /// Adds edge between artifact nodes - <see cref="from"/> and <see cref="to"/>
    /// </summary>
    /// <param name="ent">Artifact entity that contains 'from' and 'to' node entities.</param>
    /// <param name="from">Node from which we need to draw edge. </param>
    /// <param name="to">Node to which we need to draw edge. </param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if adding edge was successful, false otherwise.</returns>
    public bool AddEdge(Entity<XenoArtifactComponent?> ent, EntityUid from, EntityUid to, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryGetIndex(ent, from, out var fromIdx) ||
            !TryGetIndex(ent, to, out var toIdx))
            return false;

        return AddEdge(ent, fromIdx.Value, toIdx.Value, dirty: dirty);
    }

    /// <summary>
    /// Adds edge between artifact nodes by indices inside node container - <see cref="fromIdx"/> and <see cref="toIdx"/>
    /// </summary>
    /// <param name="ent">Artifact entity that contains 'from' and 'to' node entities.</param>
    /// <param name="fromIdx">Node index inside artifact node container, from which we need to draw edge. </param>
    /// <param name="toIdx">Node index inside artifact node container, to which we need to draw edge. </param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if adding edge was successful, false otherwise.</returns>
    public bool AddEdge(Entity<XenoArtifactComponent?> ent, int fromIdx, int toIdx, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        DebugTools.Assert(fromIdx >= 0 && fromIdx < ent.Comp.NodeVertices.Length, $"fromIdx is out of bounds for fromIdx {fromIdx}");
        DebugTools.Assert(toIdx >= 0 && toIdx < ent.Comp.NodeVertices.Length, $"toIdx is out of bounds for toIdx {toIdx}");

        if (ent.Comp.NodeAdjacencyMatrix[fromIdx][toIdx])
            return false; //Edge already exists

        // TODO: add a safety check to prohibit cyclic paths.

        ent.Comp.NodeAdjacencyMatrix[fromIdx][toIdx] = true;
        if (dirty)
        {
            RebuildXenoArtifactMetaData(ent);
        }

        return true;
    }

    /// <summary>
    /// Removes edge between artifact nodes.
    /// </summary>
    /// <param name="ent">Artifact entity that contains 'from' and 'to' node entities.</param>
    /// <param name="from">Entity of node from which edge to remove is connected.</param>
    /// <param name="to">Entity of node to which edge to remove is connected.</param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if removed edge was successfully, false otherwise.</returns>
    public bool RemoveEdge(Entity<XenoArtifactComponent?> ent, EntityUid from, EntityUid to, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryGetIndex(ent, from, out var fromIdx) ||
            !TryGetIndex(ent, to, out var toIdx))
            return false;

        return RemoveEdge(ent, fromIdx.Value, toIdx.Value, dirty);
    }

    /// <summary>
    /// Removes edge between artifact nodes.
    /// </summary>
    /// <param name="ent">Artifact entity that contains 'from' and 'to' node entities.</param>
    /// <param name="fromIdx"> First node index inside artifact node container, from which we need to remove connecting edge. </param>
    /// <param name="toIdx"> Other node index inside artifact node container, from which we need to remove connecting edge. </param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if removed edge was successfully, false otherwise.</returns>
    public bool RemoveEdge(Entity<XenoArtifactComponent?> ent, int fromIdx, int toIdx, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        DebugTools.Assert(fromIdx >= 0 && fromIdx < ent.Comp.NodeVertices.Length, $"fromIdx is out of bounds for fromIdx {fromIdx}");
        DebugTools.Assert(toIdx >= 0 && toIdx < ent.Comp.NodeVertices.Length, $"toIdx is out of bounds for toIdx {toIdx}");

        if (!ent.Comp.NodeAdjacencyMatrix[fromIdx][toIdx])
            return false; //Edge doesn't exist

        ent.Comp.NodeAdjacencyMatrix[fromIdx][toIdx] = false;

        if (dirty)
        {
            RebuildXenoArtifactMetaData(ent);
        }

        return true;
    }

    /// <summary>
    /// Creates node entity (spawns) and adds node into artifact node container.
    /// </summary>
    /// <param name="ent">Artifact entity, to container of which node should be added.</param>
    /// <param name="entProtoId">EntProtoId of node to be added.</param>
    /// <param name="node">Created node or null.</param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if node creation and adding was successful, false otherwise.</returns>
    public bool AddNode(
        Entity<XenoArtifactComponent?> ent,
        EntProtoId entProtoId,
        [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node,
        bool dirty = true
    )
    {
        node = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var uid = Spawn(entProtoId);
        node = (uid, XenoArtifactNode(uid));
        return AddNode(ent, (node.Value, node.Value.Comp), dirty: dirty);
    }

    /// <summary>
    /// Adds node entity to artifact node container.
    /// </summary>
    /// <param name="ent">Artifact entity, to container of which node should be added.</param>
    /// <param name="node">Node entity to add.</param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if node adding was successful, false otherwise.</returns>
    public bool AddNode(Entity<XenoArtifactComponent?> ent, Entity<XenoArtifactNodeComponent?> node, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        node.Comp ??= XenoArtifactNode(node);
        node.Comp.Attached = GetNetEntity(ent);

        var nodeIdx = GetFreeNodeIndex((ent, ent.Comp));
        _container.Insert(node.Owner, ent.Comp.NodeContainer);
        ent.Comp.NodeVertices[nodeIdx] = GetNetEntity(node);

        Dirty(node);
        if (dirty)
        {
            RebuildXenoArtifactMetaData(ent);
        }

        return true;
    }

    /// <summary>
    /// Removes artifact node from artifact node container.
    /// </summary>
    /// <param name="ent">Artifact from container of which node should be removed</param>
    /// <param name="node">Node entity to be removed.</param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    /// <returns>True if node was removed successfully, false otherwise.</returns>
    public bool RemoveNode(Entity<XenoArtifactComponent?> ent, Entity<XenoArtifactNodeComponent?> node, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        node.Comp ??= XenoArtifactNode(node);

        if (!TryGetIndex(ent, node, out var idx))
            return false; // node isn't attached to this entity.

        RemoveAllNodeEdges(ent, idx.Value, dirty: false);

        _container.Remove(node.Owner, ent.Comp.NodeContainer);
        node.Comp.Attached = null;
        ent.Comp.NodeVertices[idx.Value] = null;
        if (dirty)
        {
            RebuildXenoArtifactMetaData(ent);
        }

        Dirty(node);

        return true;
    }

    /// <summary>
    /// Remove edges, connected to passed artifact node.
    /// </summary>
    /// <param name="ent">Entity of artifact, in node container of which node resides.</param>
    /// <param name="nodeIdx">Index of node (inside node container), for which all edges should be removed.</param>
    /// <param name="dirty">
    /// Marker, if we need to recalculate caches and mark related components dirty to update on client side.
    /// Should be disabled for initial graph creation to not recalculate cache on each node/edge.
    /// </param>
    public void RemoveAllNodeEdges(Entity<XenoArtifactComponent?> ent, int nodeIdx, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var predecessors = GetDirectPredecessorNodes(ent, nodeIdx);
        foreach (var p in predecessors)
        {
            RemoveEdge(ent, p, nodeIdx, dirty: false);
        }

        var successors = GetDirectSuccessorNodes(ent, nodeIdx);
        foreach (var s in successors)
        {
            RemoveEdge(ent, nodeIdx, s, dirty: false);
        }

        if (dirty)
        {
            RebuildXenoArtifactMetaData(ent);
        }
    }

    /// <summary>
    /// Gets set of node entities, that are direct predecessors to passed node entity.
    /// </summary>
    /// <remarks>
    /// Direct predecessors are nodes, which are connected by edges directly to target node,
    /// and are on outgoing ('FROM') side of edge connection.
    /// </remarks>
    public HashSet<Entity<XenoArtifactNodeComponent>> GetDirectPredecessorNodes(Entity<XenoArtifactComponent?> ent, EntityUid node)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        if (!TryGetIndex(ent, node, out var index))
            return new();

        var indices = GetDirectPredecessorNodes(ent, index.Value);
        var output = new HashSet<Entity<XenoArtifactNodeComponent>>();
        foreach (var i in indices)
        {
            if (TryGetNode(ent, i, out var predecessor))
                output.Add(predecessor.Value);
        }

        return output;
    }

    /// <summary>
    /// Gets set of node indices (in artifact node container) which are direct predecessors to node with passed node index.
    /// </summary>
    /// <remarks>
    /// Direct predecessors are nodes, which are connected by edges directly to target node,
    /// and are on outgoing ('FROM') side of edge connection.
    /// </remarks>
    public HashSet<int> GetDirectPredecessorNodes(Entity<XenoArtifactComponent?> ent, int nodeIdx)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        DebugTools.Assert(nodeIdx >= 0 && nodeIdx < ent.Comp.NodeVertices.Length, $"node index {nodeIdx} is out of bounds!");

        var indices = new HashSet<int>();
        for (var i = 0; i < ent.Comp.NodeAdjacencyMatrixRows; i++)
        {
            if (ent.Comp.NodeAdjacencyMatrix[i][nodeIdx])
                indices.Add(i);
        }

        return indices;
    }

    /// <summary>
    /// Gets set of node entities, that are direct successors to passed node entity.
    /// </summary>
    /// <remarks>
    /// Direct successors are nodes, which are connected by edges
    /// directly to target node, and are on incoming ('TO') side of edge connection.
    /// </remarks>
    public HashSet<Entity<XenoArtifactNodeComponent>> GetDirectSuccessorNodes(Entity<XenoArtifactComponent?> ent, EntityUid node)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        if (!TryGetIndex(ent, node, out var index))
            return new();

        var indices = GetDirectSuccessorNodes(ent, index.Value);
        var output = new HashSet<Entity<XenoArtifactNodeComponent>>();
        foreach (var i in indices)
        {
            if (TryGetNode(ent, i, out var successor))
                output.Add(successor.Value);
        }

        return output;
    }

    /// <summary>
    /// Gets set of node indices (in artifact node container) which are direct successors to node with passed node index.
    /// </summary>
    /// <remarks>
    /// Direct successors are nodes, which are connected by edges
    /// directly to target node, and are on incoming ('TO') side of edge connection.
    /// </remarks>
    public HashSet<int> GetDirectSuccessorNodes(Entity<XenoArtifactComponent?> ent, int nodeIdx)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();
        DebugTools.Assert(nodeIdx >= 0 && nodeIdx < ent.Comp.NodeVertices.Length, "node index is out of bounds!");

        var indices = new HashSet<int>();
        for (var i = 0; i < ent.Comp.NodeAdjacencyMatrixColumns; i++)
        {
            if (ent.Comp.NodeAdjacencyMatrix[nodeIdx][i])
                indices.Add(i);
        }

        return indices;
    }

    /// <summary>
    /// Gets set of node entities, that are predecessors to passed node entity.
    /// </summary>
    /// <remarks>
    /// Predecessors are nodes, which are connected by edges directly to target node on 'FROM' side of edge,
    /// or connected to such node on 'FROM' side of edge, etc recursively.
    /// </remarks>
    public HashSet<Entity<XenoArtifactNodeComponent>> GetPredecessorNodes(Entity<XenoArtifactComponent?> ent, Entity<XenoArtifactNodeComponent> node)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        var predecessors = GetPredecessorNodes(ent, GetIndex((ent, ent.Comp), node));
        var output = new HashSet<Entity<XenoArtifactNodeComponent>>();
        foreach (var p in predecessors)
        {
            output.Add(GetNode((ent, ent.Comp), p));
        }

        return output;
    }

    /// <summary>
    /// Gets set of node indices inside artifact node container, that are predecessors to entity with passed node index.
    /// </summary>
    /// <remarks>
    /// Predecessors are nodes, which are connected by edges directly to target node on 'FROM' side of edge,
    /// or connected to such node on 'FROM' side of edge, etc recursively.
    /// </remarks>
    public HashSet<int> GetPredecessorNodes(Entity<XenoArtifactComponent?> ent, int nodeIdx)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        var predecessors = GetDirectPredecessorNodes(ent, nodeIdx);
        if (predecessors.Count == 0)
            return new();

        var output = new HashSet<int>();
        foreach (var p in predecessors)
        {
            output.Add(p);
            var recursivePredecessors = GetPredecessorNodes(ent, p);
            foreach (var rp in recursivePredecessors)
            {
                output.Add(rp);
            }
        }

        return output;
    }

    /// <summary>
    /// Gets set of node entities, that are successors to passed node entity.
    /// </summary>
    /// <remarks>
    /// Successors are nodes, which are connected by edges directly to target node on 'TO' side of edge,
    /// or connected to such node on 'TO' side of edge, etc recursively.
    /// </remarks>
    public HashSet<Entity<XenoArtifactNodeComponent>> GetSuccessorNodes(Entity<XenoArtifactComponent?> ent, Entity<XenoArtifactNodeComponent> node)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        var successors = GetSuccessorNodes(ent, GetIndex((ent, ent.Comp), node));
        var output = new HashSet<Entity<XenoArtifactNodeComponent>>();
        foreach (var s in successors)
        {
            output.Add(GetNode((ent, ent.Comp), s));
        }

        return output;
    }

    /// <summary>
    /// Gets set of node indices inside artifact node container, that are successors to entity with passed node index.
    /// </summary>
    /// <remarks>
    /// Successors are nodes, which are connected by edges directly to target node on 'TO' side of edge,
    /// or connected to such node on 'TO' side of edge, etc recursively.
    /// </remarks>
    public HashSet<int> GetSuccessorNodes(Entity<XenoArtifactComponent?> ent, int nodeIdx)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        var successors = GetDirectSuccessorNodes(ent, nodeIdx);
        if (successors.Count == 0)
            return new();

        var output = new HashSet<int>();
        foreach (var s in successors)
        {
            output.Add(s);
            var recursiveSuccessors = GetSuccessorNodes(ent, s);
            foreach (var rs in recursiveSuccessors)
            {
                output.Add(rs);
            }
        }

        return output;
    }

    /// <summary>
    /// Determines, if there is an edge (directed link) FROM one node TO other in passed artifact.
    /// </summary>
    /// <param name="ent">Artifact, inside which node container nodes are.</param>
    /// <param name="from">Node FROM which existence of edge should be checked.</param>
    /// <param name="to">Node TO which existance of edge should be checked.</param>
    public bool NodeHasEdge(
        Entity<XenoArtifactComponent?> ent,
        Entity<XenoArtifactNodeComponent?> from,
        Entity<XenoArtifactNodeComponent?> to
    )
    {
        if (!Resolve(ent, ref ent.Comp))
            return new();

        var fromIdx = GetIndex((ent, ent.Comp), from);
        var toIdx = GetIndex((ent, ent.Comp), to);

        return ent.Comp.NodeAdjacencyMatrix[fromIdx][toIdx];
    }

    /// <summary>
    /// Resizes the adjacency matrix and vertices array to <paramref name="newSize"/>,
    /// or at least what it WOULD do if i wasn't forced to use shitty lists.
    /// </summary>
    protected void ResizeNodeGraph(Entity<XenoArtifactComponent> ent, int newSize)
    {
        Array.Resize(ref ent.Comp.NodeVertices, newSize);

        while (ent.Comp.NodeAdjacencyMatrix.Count < newSize)
        {
            ent.Comp.NodeAdjacencyMatrix.Add(new());
        }

        foreach (var row in ent.Comp.NodeAdjacencyMatrix)
        {
            while (row.Count < newSize)
            {
                row.Add(false);
            }
        }

        Dirty(ent);
    }

    /// <summary> Removes unlocking state from artifact. </summary>
    private void CancelUnlockingOnGraphStructureChange(Entity<XenoArtifactComponent> ent)
    {
        if (!TryComp<XenoArtifactUnlockingComponent>(ent, out var unlockingComponent))
            return;

        Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> artifactEnt = (ent, unlockingComponent, ent.Comp);
        CancelUnlockingState(artifactEnt);
    }
}
