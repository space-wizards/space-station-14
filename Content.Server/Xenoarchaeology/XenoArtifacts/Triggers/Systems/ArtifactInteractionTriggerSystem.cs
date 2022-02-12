using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Physics.Pull;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public class ArtifactInteractionTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, PullStartedMessage>(OnPull);
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, AttackedEvent>(OnAttack);
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, InteractHandEvent>(OnInteract);
    }

    private void OnPull(EntityUid uid, ArtifactInteractionTriggerComponent component, PullStartedMessage args)
    {
        if (!component.PullActivation)
            return;

        _artifactSystem.TryActivateArtifact(uid, args.Puller.Owner);
    }

    private void OnAttack(EntityUid uid, ArtifactInteractionTriggerComponent component, AttackedEvent args)
    {
        if (!component.AttackActivation)
            return;

        _artifactSystem.TryActivateArtifact(uid, args.User);
    }

    private void OnInteract(EntityUid uid, ArtifactInteractionTriggerComponent component, InteractHandEvent args)
    {
        if (args.Handled || !args.InRangeUnobstructed())
            return;

        if (!component.EmptyHandActivation)
            return;

        args.Handled = _artifactSystem.TryActivateArtifact(uid, args.User);
    }
}
