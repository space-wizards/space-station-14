using Content.Shared.Chemistry;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public sealed class XATReactiveSystem : BaseXATSystem<XATReactiveComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<ReactionEntityEvent>(OnReaction);
    }

    private void OnReaction(Entity<XenoArtifactComponent> artifact, Entity<XATReactiveComponent, XenoArtifactNodeComponent> node, ref ReactionEntityEvent args)
    {
        var artefactReactiveTriggerComponent = node.Comp1;
        if (!artefactReactiveTriggerComponent.ReactionMethods.Contains(args.Method))
            return;

        if (args.ReagentQuantity.Quantity < artefactReactiveTriggerComponent.MinQuantity)
            return;

        if (!artefactReactiveTriggerComponent.Reagents.Contains(args.Reagent.ID))
            return;

        Trigger(artifact, node);
    }
}
