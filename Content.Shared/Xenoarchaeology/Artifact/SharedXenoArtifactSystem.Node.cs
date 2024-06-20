using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    private EntityQuery<XenoArtifactNodeComponent> _nodeQuery;

    public void InitializeNode()
    {
        SubscribeLocalEvent<XenoArtifactNodeComponent, MapInitEvent>(OnNodeMapInit);

        _nodeQuery = GetEntityQuery<XenoArtifactNodeComponent>();
    }

    private void OnNodeMapInit(Entity<XenoArtifactNodeComponent> ent, ref MapInitEvent args)
    {
        ReplenishNodeDurability((ent, ent));
    }

    public XenoArtifactNodeComponent XenoArtifactNode(EntityUid uid)
    {
        return _nodeQuery.Get(uid);
    }

    public void SetNodeUnlocked(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Locked)
            return;

        ent.Comp.Locked = false;
        if (ent.Comp.Attached is { } artifact)
            RebuildCachedActiveNodes(artifact);
        Dirty(ent);
    }

    /// <summary>
    /// Resets a node's durability back to max.
    /// </summary>
    public void ReplenishNodeDurability(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        SetNodeDurability(ent, ent.Comp.MaxDurability);
    }

    /// <summary>
    /// Adds to the nodes durability by the specified value.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="delta"></param>
    public void AdjustNodeDurability(Entity<XenoArtifactNodeComponent?> ent, int delta)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        SetNodeDurability(ent, ent.Comp.Durability + delta);
    }

    /// <summary>
    /// Sets a node's durability to the specified value.
    /// </summary>
    public void SetNodeDurability(Entity<XenoArtifactNodeComponent?> ent, int durability)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        ent.Comp.Durability = Math.Clamp(durability, 0, ent.Comp.MaxDurability);
        Dirty(ent);
    }

    /// <summary>
    /// Clears all cached active nodes and rebuilds the list using the current node state.
    /// Active nodes have the following property:
    /// - Are unlocked
    /// - Have no successors which are also locked
    /// </summary>
    /// <remarks>
    /// You could technically modify this to have a per-node method that only checks direct predecessors
    /// and then does recursive updates for all successors, but I don't think the optimization is necessary right now.
    /// </remarks>
    public void RebuildCachedActiveNodes(Entity<XenoArtifactComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.CachedActiveNodes.Clear();
        var allNodes = GetAllNodes((ent, ent.Comp));
        foreach (var node in allNodes)
        {
            // Locked nodes cannot be active.
            if (node.Comp.Locked)
                continue;

            var successors = GetDirectSuccessorNodes(ent, node);

            // If this node has no successors, then we don't need to bother with this extra logic.
            if (successors.Count != 0)
            {
                // Checks for any of the direct successors being unlocked.
                var successorIsUnlocked = false;
                foreach (var sNode in successors)
                {
                    if (sNode.Comp.Locked)
                        continue;
                    successorIsUnlocked = true;
                    break;
                }

                // Active nodes must be at the end of the path.
                if (successorIsUnlocked)
                    continue;
            }

            ent.Comp.CachedActiveNodes.Add(GetNetEntity(node));
        }

        Dirty(ent);
    }
}
