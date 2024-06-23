using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public abstract class BaseXATSystem<T> : EntitySystem where T : Component
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedXenoArtifactSystem XenoArtifact = default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<T, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTriggerCrude(artifact))
                continue;

            if (!XenoArtifact.CanUnlockNode((uid, node)))
                continue;

            UpdateXAT(artifact, (uid, comp, node), frameTime);
        }
    }

    private bool CanTriggerCrude(Entity<XenoArtifactComponent> artifact)
    {
        return _timing.CurTime > artifact.Comp.NextUnlockTime;
    }

    protected virtual void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<T, XenoArtifactNodeComponent> node, float frameTime)
    {

    }

    protected void Trigger(Entity<XenoArtifactComponent> artifact, Entity<T, XenoArtifactNodeComponent> node)
    {
        XenoArtifact.TriggerXenoArtifact(artifact, (node.Owner, node.Comp2));
    }
}
