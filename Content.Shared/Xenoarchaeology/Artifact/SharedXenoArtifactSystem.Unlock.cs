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

        if (artiComp.Suppressed)
            return false;

        if (!HasUnlockedPredecessor((artifact.Value, artiComp), ent))
            return false;

        return true;
    }

    public void FinishUnlockingState(Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent)
    {
        string unlockAttemptResultMsg;
        var artifactComponent = ent.Comp2;
        if (TryGetNodeFromUnlockState(ent, out var node))
        {
            // TODO: animation
            SetNodeUnlocked((ent, artifactComponent), node.Value);
            unlockAttemptResultMsg = "artifact-unlock-state-end-success";
            ActivateNode((ent, artifactComponent), node.Value, null, null, Transform(ent).Coordinates, false);
        }
        else
        {
            unlockAttemptResultMsg = "artifact-unlock-state-end-failure";
        }

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString(unlockAttemptResultMsg), ent);

        var unlockingComponent = ent.Comp1;
        RemComp(ent, unlockingComponent);
        artifactComponent.NextUnlockTime = _timing.CurTime + artifactComponent.UnlockStateRefractory;
    }

    /// <summary>
    /// Gets first locked node that can be unlocked (it is locked and all predecessor are unlocked).
    /// </summary>
    public bool TryGetNodeFromUnlockState(
        Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent,
        [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node
    )
    {
        node = null;

        var artifactUnlockingComponent = ent.Comp1;
        foreach (var nodeIndex in artifactUnlockingComponent.TriggeredNodeIndexes)
        {
            var artifactComponent = ent.Comp2;
            var curNode = GetNode((ent, artifactComponent), nodeIndex);
            if (!curNode.Comp.Locked || !CanUnlockNode((curNode, curNode)))
                continue;

            var requiredIndices = GetPredecessorNodes((ent, artifactComponent), nodeIndex);
            requiredIndices.Add(nodeIndex);

            // Make sure the two sets are identical
            if (requiredIndices.Count != artifactUnlockingComponent.TriggeredNodeIndexes.Count
                || !artifactUnlockingComponent.TriggeredNodeIndexes.All(requiredIndices.Contains))
                continue;

            node = curNode;
            return true;
        }

        return node != null;
    }
}
