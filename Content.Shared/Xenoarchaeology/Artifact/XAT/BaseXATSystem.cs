using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public abstract class BaseXATSystem<T> : EntitySystem where T : Component
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedXenoArtifactSystem XenoArtifact = default!;

    private EntityQuery<XenoArtifactUnlockingComponent> _unlockingQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _unlockingQuery = GetEntityQuery<XenoArtifactUnlockingComponent>();
    }

    protected void XATSubscribeDirectEvent<TEvent>(XATEventHandler<TEvent> eventHandler) where TEvent : notnull
    {
        SubscribeLocalEvent<T, XenoArchNodeRelayedEvent<TEvent>>((uid, component, args) =>
        {
            var nodeComp = Comp<XenoArtifactNodeComponent>(uid);

            if (!CanTrigger(args.Artifact, (uid, nodeComp)))
                return;

            var node = new Entity<T, XenoArtifactNodeComponent>(uid, component, nodeComp);
            eventHandler.Invoke(args.Artifact, node, ref args.Args);
        });
    }

    protected bool CanTrigger(Entity<XenoArtifactComponent> artifact, Entity<XenoArtifactNodeComponent> node)
    {
        if (Timing.CurTime < artifact.Comp.NextUnlockTime)
            return false;

        if (_unlockingQuery.TryComp(artifact, out var unlocking) &&
            unlocking.TriggeredNodeIndexes.Contains(XenoArtifact.GetIndex(artifact, node)))
            return false;

        if (!XenoArtifact.CanUnlockNode((node, node)))
            return false;

        return true;
    }

    protected void Trigger(Entity<XenoArtifactComponent> artifact, Entity<T, XenoArtifactNodeComponent> node)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        Log.Debug($"Activated trigger {typeof(T).Name} on node {ToPrettyString(node)} for {ToPrettyString(artifact)}");
        XenoArtifact.TriggerXenoArtifact(artifact, (node.Owner, node.Comp2));
    }

    protected delegate void XATEventHandler<TEvent>(
        Entity<XenoArtifactComponent> artifact,
        Entity<T, XenoArtifactNodeComponent> node,
        ref TEvent args
    ) where TEvent : notnull;
}
