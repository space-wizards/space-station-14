using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some way of 'using' (with default action) an artifact entity.
/// </summary>
public sealed class XATInteractionSystem : BaseXATSystem<XATInteractionComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<PullStartedMessage>(OnPullStart);
        XATSubscribeDirectEvent<AttackedEvent>(OnAttacked);
        XATSubscribeDirectEvent<InteractHandEvent>(OnInteractHand);
    }

    private void OnPullStart(Entity<XenoArtifactComponent> artifact, Entity<XATInteractionComponent, XenoArtifactNodeComponent> node, ref PullStartedMessage args)
    {
        Trigger(artifact, node);
    }

    private void OnAttacked(Entity<XenoArtifactComponent> artifact, Entity<XATInteractionComponent, XenoArtifactNodeComponent> node, ref AttackedEvent args)
    {
        Trigger(artifact, node);
    }

    private void OnInteractHand(Entity<XenoArtifactComponent> artifact, Entity<XATInteractionComponent, XenoArtifactNodeComponent> node, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Trigger(artifact, node);
    }
}
