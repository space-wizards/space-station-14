using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Sets an artifact into the unlocking state and marks the artifexium effect as true.
/// This is a very specific behavior intended for a specific chem.
/// </summary>
public sealed partial class ArtifactUnlock : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entMan = args.EntityManager;
        var xenoArtifactSys = entMan.System<SharedXenoArtifactSystem>();
        var popupSys = entMan.System<SharedPopupSystem>();

        if (!entMan.TryGetComponent<XenoArtifactComponent>(args.TargetEntity, out var xenoArtifact))
            return;

        if (!entMan.TryGetComponent<XenoArtifactUnlockingComponent>(args.TargetEntity, out var unlocking))
        {
            xenoArtifactSys.TriggerXenoArtifact((args.TargetEntity, xenoArtifact), null, force: true);
            unlocking = entMan.EnsureComponent<XenoArtifactUnlockingComponent>(args.TargetEntity);
        }
        else if (!unlocking.ArtifexiumApplied)
        {
            popupSys.PopupEntity(Loc.GetString("artifact-activation-artifexium"), args.TargetEntity, PopupType.Medium);
        }

        if (unlocking.ArtifexiumApplied)
            return;

        xenoArtifactSys.SetArtifexiumApplied((args.TargetEntity, unlocking), true);
    }

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-artifact-unlock", ("chance", Probability));
    }
}
