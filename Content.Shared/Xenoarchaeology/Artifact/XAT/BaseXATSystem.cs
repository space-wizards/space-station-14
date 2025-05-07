using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// Base type for xeno artifact trigger systems. Each system should work with 1 trigger mechanics.
/// </summary>
/// <typeparam name="T">Type of XAT component that system will work with.</typeparam>
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

    /// <summary>
    /// Subscribes to event occurring on artifact (and by relaying - on node).
    /// </summary>
    /// <typeparam name="TEvent">Type of event to sub for.</typeparam>
    /// <param name="eventHandler">Delegate that handles event.</param>
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

    /// <summary>
    /// Checks if node can be triggered.
    /// </summary>
    /// <param name="artifact">Artifact entity.</param>
    /// <param name="node">Node from <see cref="artifact"/>.</param>
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

    /// <summary>
    /// Triggers node. Triggered nodes participate in node unlocking.
    /// </summary>
    protected void Trigger(Entity<XenoArtifactComponent> artifact, Entity<T, XenoArtifactNodeComponent> node)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        Log.Debug($"Activated trigger {typeof(T).Name} on node {ToPrettyString(node)} for {ToPrettyString(artifact)}");
        XenoArtifact.TriggerXenoArtifact(artifact, (node.Owner, node.Comp2));
    }

    /// <summary>
    /// Delegate for handling relayed artifact trigger events.
    /// </summary>
    /// <typeparam name="TEvent">Event type to be handled.</typeparam>
    /// <param name="artifact">Artifact, on which event occurred.</param>
    /// <param name="node">Node which for which event were relayed.</param>
    /// <param name="args">Event data.</param>
    protected delegate void XATEventHandler<TEvent>(
        Entity<XenoArtifactComponent> artifact,
        Entity<T, XenoArtifactNodeComponent> node,
        ref TEvent args
    ) where TEvent : notnull;
}
