using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.InfectionDead.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CureInfectionDead : ReagentEffect
{

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("Лечит некроинфекцию", ("chance", Probability));
    }

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        //RemCompDeferred
        entityManager.RemoveComponent<InfectionDeadComponent>(args.SolutionEntity);
    }
}

