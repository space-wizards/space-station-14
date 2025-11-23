using Content.Shared.Chemistry;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some chemical reagent.
/// </summary>
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
        var reactiveTriggerComponent = node.Comp1;
        if (!reactiveTriggerComponent.ReactionMethods.Contains(args.Method))
            return;

        if (args.ReagentQuantity.Quantity < reactiveTriggerComponent.MinQuantity)
            return;

        if (!reactiveTriggerComponent.Reagents.Contains(args.Reagent.ID))
            return;

        if (reactiveTriggerComponent.ReactiveGroups?.Count > 0 && !ReagentHaveReactiveGroup(args, reactiveTriggerComponent))
            return;

        Trigger(artifact, node);
    }

    private static bool ReagentHaveReactiveGroup(ReactionEntityEvent args, XATReactiveComponent reactiveTriggerComponent)
    {
        var reactiveReagentEffectEntries = args.Reagent.ReactiveEffects;
        if (reactiveReagentEffectEntries == null)
        {
            return false;
        }

        var reactiveGroups = reactiveTriggerComponent.ReactiveGroups;
        foreach(var reactiveGroup in reactiveGroups)
        {
            if (reactiveReagentEffectEntries.TryGetValue(reactiveGroup, out var effectEntry)
                && effectEntry.Methods?.Contains(args.Method) == true)
            {
                return true;
            }
        }

        return false;
    }
}
