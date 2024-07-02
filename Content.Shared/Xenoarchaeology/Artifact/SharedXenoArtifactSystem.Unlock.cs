using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    private EntityQuery<XenoArtifactUnlockingComponent> _unlockingQuery;

    // TODO: we should cancel the unlock state if you remove/add nodes
    private void InitializeUnlock()
    {
        _unlockingQuery = GetEntityQuery<XenoArtifactUnlockingComponent>();
    }

    private void UpdateUnlock(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoArtifactUnlockingComponent, XenoArtifactComponent>();
        while (query.MoveNext(out var uid, out var unlock, out var comp))
        {
            if (_timing.CurTime < unlock.EndTime)
                continue;

            FinishUnlockingState((uid, unlock, comp));
        }
    }

    public bool CanUnlockNode(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var artifact = GetEntity(ent.Comp.Attached);
        if (!TryComp<XenoArtifactComponent>(artifact, out var artiComp))
            return false;

        if (!HasUnlockedPredecessor((artifact.Value, artiComp), ent))
            return false;

        return true;
    }

    public void FinishUnlockingState(Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent)
    {
        string unlockMsg;
        if (TryGetNodeFromUnlockState(ent, out var node))
        {
            // TODO: animation
            SetNodeUnlocked((ent, ent.Comp2), node.Value);
            unlockMsg = "artifact-unlock-state-end-success";
        }
        else
        {
            unlockMsg = "artifact-unlock-state-end-failure";
        }

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString(unlockMsg), ent);

        RemComp(ent, ent.Comp1);
        ent.Comp2.NextUnlockTime = _timing.CurTime + ent.Comp2.UnlockStateRefractory;
    }

    public bool TryGetNodeFromUnlockState(
        Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent,
        [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node)
    {
        node = null;

        foreach (var nodeIndex in ent.Comp1.TriggeredNodeIndexes)
        {
            var curNode = GetNode((ent, ent.Comp2), nodeIndex);
            if (!curNode.Comp.Locked || !CanUnlockNode((curNode, curNode)))
                continue;

            var neededIndices = GetPredecessorNodes((ent, ent.Comp2), nodeIndex);
            neededIndices.Add(nodeIndex);

            // Make sure the two sets are identical
            if (neededIndices.Count != ent.Comp1.TriggeredNodeIndexes.Count ||
                !ent.Comp1.TriggeredNodeIndexes.All(neededIndices.Contains))
                continue;

            node = curNode;
            return true;
        }

        return node != null;
    }
}
