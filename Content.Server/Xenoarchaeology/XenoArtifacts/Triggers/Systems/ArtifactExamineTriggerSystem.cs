using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Examine;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed partial class ArtifactExamineTriggerSystem : EntitySystem
{
    [Dependency] private ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactExamineTriggerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, ArtifactExamineTriggerComponent component, ExaminedEvent args)
    {
        _artifact.TryActivateArtifact(uid);
    }
}
