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
        if (!node.Comp1.ReactionMethods.Contains(args.Method))
            return;

        if (args.ReagentQuantity.Quantity < node.Comp1.MinQuantity)
            return;

        if (!node.Comp1.Reagents.Contains(args.Reagent.ID))
            return;

        Trigger(artifact, node);
    }
}
