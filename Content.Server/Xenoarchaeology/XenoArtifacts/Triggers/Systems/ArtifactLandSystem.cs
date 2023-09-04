using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Throwing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

[InjectDependencies]
public sealed partial class ArtifactLandSystem : EntitySystem
{
    [Dependency] private ArtifactSystem _artifact = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactLandTriggerComponent, LandEvent>(OnLand);
    }

    private void OnLand(EntityUid uid, ArtifactLandTriggerComponent component, ref LandEvent args)
    {
        _artifact.TryActivateArtifact(uid, args.User);
    }
}
