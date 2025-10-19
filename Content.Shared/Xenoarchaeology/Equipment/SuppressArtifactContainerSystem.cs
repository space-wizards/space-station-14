using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Xenoarchaeology.Equipment;

public sealed class SuppressArtifactContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoArtifactSystem _xenoArtifact = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(EntityUid uid, SuppressArtifactContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<XenoArtifactComponent>(args.Entity, out var artifact))
            return;

        _xenoArtifact.SetSuppressed((args.Entity, artifact), true);
    }

    private void OnRemoved(EntityUid uid, SuppressArtifactContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (!TryComp<XenoArtifactComponent>(args.Entity, out var artifact))
            return;

        _xenoArtifact.SetSuppressed((args.Entity, artifact), false);
    }
}
