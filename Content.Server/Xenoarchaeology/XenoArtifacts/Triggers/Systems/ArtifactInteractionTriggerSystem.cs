using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
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
        if (args.Handled)
            return;
        if (!args.InRangeUnobstructed())
            return;

        args.Handled = _artifactSystem.TryActivateArtifact(uid, args.User);
    }
}
