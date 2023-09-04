using Content.Server.Kitchen.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

[InjectDependencies]
public sealed partial class ArtifactMicrowaveTriggerSystem : EntitySystem
{
    [Dependency] private ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactMicrowaveTriggerComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(EntityUid uid, ArtifactMicrowaveTriggerComponent component, BeingMicrowavedEvent args)
    {
        _artifact.TryActivateArtifact(uid);
    }
}
