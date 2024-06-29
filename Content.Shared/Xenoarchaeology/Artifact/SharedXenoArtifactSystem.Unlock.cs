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
            //todo: trigger unlock
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
}
