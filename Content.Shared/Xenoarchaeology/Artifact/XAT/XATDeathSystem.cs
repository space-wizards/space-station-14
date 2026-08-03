using Content.Shared.Mobs;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires death of some mob near artifact.
/// </summary>
public sealed partial class XATDeathSystem : BaseXATSystem<XATDeathComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        // The incoming state from the server raises the event as well.
        // But the changes have also been dirtied
        // so we prevent applying them twice.
        if (_timing.ApplyingState)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        var targetCoords = Transform(args.Target).Coordinates;

        var query = EntityQueryEnumerator<XATDeathComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (_transform.InRange(targetCoords, artifactCoords, comp.Range))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
