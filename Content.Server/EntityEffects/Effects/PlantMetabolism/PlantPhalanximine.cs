using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlantPhalanximine : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null)
            return;
        var plantComp = args.EntityManager.GetComponent<PlantComponent>(plantHolderComp.PlantUid.Value);
        if (plantComp == null || plantComp.Dead || plantComp.Seed == null || plantComp.Seed.Immutable)
            return;

        plantComp.Seed.Viable = true;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-phalanximine", ("chance", Probability));
}
