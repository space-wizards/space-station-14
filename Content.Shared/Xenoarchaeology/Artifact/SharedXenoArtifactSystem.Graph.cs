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
    /// Gets the index corresponding to a given node, throwing if the node is not present.
    /// </summary>
    public int GetIndex(Entity<XenoArtifactComponent> ent, EntityUid node)
    {
        if (TryGetIndex((ent, ent), node, out var index))
        {
            return index.Value;
        }

        throw new ArgumentException($"node {ToPrettyString(node)} is not present in {ToPrettyString(ent)}");
    }

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

    public Entity<XenoArtifactNodeComponent> GetNode(Entity<XenoArtifactComponent> ent, int index)
    {
        if (ent.Comp.NodeVertices[index] is { } netUid && GetEntity(netUid) is var uid)
            return (uid, XenoArtifactNode(uid));

        throw new ArgumentException($"index {index} does not correspond to an existing node in {ToPrettyString(ent)}");
    }

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

    public IEnumerable<Entity<XenoArtifactNodeComponent>> GetAllNodes(Entity<XenoArtifactComponent> ent)
    {
        foreach (var netNode in ent.Comp.NodeVertices)
        {
            if (GetEntity(netNode) is { } node)
                yield return (node, XenoArtifactNode(node));
        }
    }

    public IEnumerable<int> GetAllNodeIndices(Entity<XenoArtifactComponent> ent)
    {
        for (var i = 0; i < ent.Comp.NodeVertices.Length; i++)
        {
            if (ent.Comp.NodeVertices[i] is not null)
                yield return i;
        }
    }

    public bool AddEdge(Entity<XenoArtifactComponent?> ent, EntityUid from, EntityUid to, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryGetIndex(ent, from, out var fromIdx) ||
            !TryGetIndex(ent, to, out var toIdx))
            return false;

        return AddEdge(ent, fromIdx.Value, toIdx.Value, dirty: dirty);
    }

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

    public bool RemoveEdge(Entity<XenoArtifactComponent?> ent, EntityUid from, EntityUid to, bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryGetIndex(ent, from, out var fromIdx) ||
            !TryGetIndex(ent, to, out var toIdx))
            return false;

        return RemoveEdge(ent, fromIdx.Value, toIdx.Value, dirty);
    }

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

    public bool AddNode(
        Entity<XenoArtifactComponent?> ent,
        EntProtoId<XenoArtifactNodeComponent> prototype,
        [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node,
        bool dirty = true
    )
    {
        node = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var uid = Spawn(prototype.Id);
        node = (uid, XenoArtifactNode(uid));
        return AddNode(ent, (node.Value, node.Value.Comp), dirty: dirty);
    }

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
    /// Resizes the adjacency matrix and vertices array to newLength
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
}
