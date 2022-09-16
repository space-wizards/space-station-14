using Content.Server.Cargo.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Equipment.Components;
using Robust.Shared.Containers;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Equipment.Systems;

public sealed class SuppressArtifactContainerSystem : EntitySystem
{
    /// <summary>
    /// Artifacts go from 2k to 4k, 1.5k net profit (considering the container price
    /// </summary>
    public const double ContainedArtifactModifier = 2;

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

        if (TryComp<StaticPriceComponent>(args.Entity, out var price))
        {
            price.Price *= ContainedArtifactModifier;
        }
    }

    private void OnRemoved(EntityUid uid, SuppressArtifactContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (!TryComp(args.Entity, out ArtifactComponent? artifact))
            return;

        artifact.IsSuppressed = false;

        if (TryComp<StaticPriceComponent>(args.Entity, out var price))
        {
            price.Price /= ContainedArtifactModifier;
        }
    }
}
