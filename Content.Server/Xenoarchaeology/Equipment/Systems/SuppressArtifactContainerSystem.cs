using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Containers;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class SuppressArtifactContainerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(EntityUid uid, SuppressArtifactContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out ArtifactComponent? artifact))
            return;

        artifact.IsSuppressed = true;
    }

    private void OnRemoved(EntityUid uid, SuppressArtifactContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (!TryComp(args.Entity, out ArtifactComponent? artifact))
            return;

        artifact.IsSuppressed = false;
    }
}
