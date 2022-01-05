using Content.Server.Xenoarchaeology.XenoArtifacts.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public class ArtifactInteractionTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, ArtifactInteractionTriggerComponent component, InteractHandEvent args)
    {
        _artifactSystem.TryActivateArtifact(uid, args.User);
    }
}
