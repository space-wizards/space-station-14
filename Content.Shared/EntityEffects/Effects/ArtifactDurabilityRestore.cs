using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Restores durability in active artefact node with some <see cref="Probability"/>.
/// </summary>
public sealed partial class ArtifactDurabilityRestore : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        if (!random.Prob(Probability))
            return;

        var entMan = args.EntityManager;
        var xenoArtifactSys = entMan.System<SharedXenoArtifactSystem>();

        if (!entMan.TryGetComponent<XenoArtifactComponent>(args.TargetEntity, out var xenoArtifact))
            return;

        var node = random.Pick(xenoArtifactSys.GetActiveNodes((args.TargetEntity, xenoArtifact)));

        if (!entMan.TryGetComponent<XenoArtifactNodeComponent>(node, out var nodeComp))
            return;

        xenoArtifactSys.AdjustNodeDurability(node.Owner, random.Next(nodeComp.MaxDurability));
    }

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-artifact-durability-restore", ("chance", Probability));
    }
}
