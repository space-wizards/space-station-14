using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Containers;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class SuppressArtifactContainerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(EntityUid uid, SuppressArtifactContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<ArtifactComponent>(args.Entity, out var artifact))
            return;

        _artifact.SetIsSuppressed(args.Entity, true, artifact);
    }

    private void OnRemoved(EntityUid uid, SuppressArtifactContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (!TryComp<ArtifactComponent>(args.Entity, out var artifact))
            return;

        _artifact.SetIsSuppressed(args.Entity, false, artifact);
    }
}
