using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.InfectionDead.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CauseInfectionDead : ReagentEffect
{

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("это пить не стоит", ("chance", Probability));
    }

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        if (!entityManager.HasComponent<MobStateComponent>(args.SolutionEntity) || !entityManager.HasComponent<HumanoidAppearanceComponent>(args.SolutionEntity) || entityManager.HasComponent<ImmunitetInfectionDeadComponent>(args.SolutionEntity))
            return;

        if (entityManager.HasComponent<InfectionDeadComponent>(args.SolutionEntity))
            return;

        entityManager.EnsureComponent<InfectionDeadComponent>(args.SolutionEntity);
    }
}

