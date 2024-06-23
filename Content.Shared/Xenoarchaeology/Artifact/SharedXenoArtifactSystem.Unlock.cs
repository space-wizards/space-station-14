using System.Linq;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    private EntityQuery<XenoArtifactUnlockingComponent> _unlockingQuery;

    private void InitializeUnlock()
    {
        _unlockingQuery = GetEntityQuery<XenoArtifactUnlockingComponent>();
    }

    private void UpdateUnlock(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoArtifactUnlockingComponent, XenoArtifactComponent>();
        while (query.MoveNext(out var unlock, out var comp))
        {
            if (_timing.CurTime < unlock.EndTime)
                continue;
        }
    }

    public void TriggerXenoArtifact(Entity<XenoArtifactComponent> ent, Entity<XenoArtifactNodeComponent> node)
    {
        if (_timing.CurTime < ent.Comp.NextUnlockTime)
            return;

        if (!_unlockingQuery.TryGetComponent(ent, out var unlockingComp))
        {
            unlockingComp = EnsureComp<XenoArtifactUnlockingComponent>(ent);
            unlockingComp.EndTime = _timing.CurTime + ent.Comp.UnlockStateDuration;
        }
        var index = GetIndex(ent, node);
        unlockingComp.TriggeredNodeIndexes.Add(index);
    }

    public bool CanUnlockNode(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryComp<XenoArtifactComponent>(ent.Comp.Attached, out var artiComp))
            return false;

        var predecessors = GetDirectPredecessorNodes((ent.Comp.Attached.Value, artiComp), ent);
        return predecessors.Any(p => !p.Comp.Locked);
    }
}
