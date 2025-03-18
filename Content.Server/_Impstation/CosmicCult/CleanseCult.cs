using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CleanseCult : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cleanse-cultist", ("chance", Probability));
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        if (entityManager.HasComponent<CosmicCultComponent>(uid) || entityManager.HasComponent<RogueAscendedInfectionComponent>(uid))
        {
            entityManager.EnsureComponent<CleanseCultComponent>(uid); // We just slap them with the component and let the Deconversion system handle the rest.
        }
    }
}
