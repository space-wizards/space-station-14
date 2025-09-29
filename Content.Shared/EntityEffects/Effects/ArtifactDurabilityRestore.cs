using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     Restores durability in active artefact nodes.
/// </summary>
public sealed partial class ArtifactDurabilityRestore : EntityEffect
{
    /// <summary>
    ///     Amount of durability that will be restored per effect interaction.
    /// </summary>
    [DataField]
    public int RestoredDurability = 1;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entMan = args.EntityManager;
        var xenoArtifactSys = entMan.System<SharedXenoArtifactSystem>();

        if (!entMan.TryGetComponent<XenoArtifactComponent>(args.TargetEntity, out var xenoArtifact))
            return;

        foreach (var node in xenoArtifactSys.GetActiveNodes((args.TargetEntity, xenoArtifact)))
        {
            xenoArtifactSys.AdjustNodeDurability(node.Owner, RestoredDurability);
        }
    }

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-artifact-durability-restore", ("restored", RestoredDurability));
    }
}
