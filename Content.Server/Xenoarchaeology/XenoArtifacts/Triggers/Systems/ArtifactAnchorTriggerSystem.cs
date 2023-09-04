using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

[InjectDependencies]
public sealed partial class ArtifactAnchorTriggerSystem : EntitySystem
{
    [Dependency] private ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactAnchorTriggerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorStateChanged(EntityUid uid, ArtifactAnchorTriggerComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Detaching)
            return;

        _artifact.TryActivateArtifact(uid);
    }
}
