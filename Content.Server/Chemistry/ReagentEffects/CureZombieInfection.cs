using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

using Robust.Shared.Configuration;
using Content.Server.Zombies;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed class CureZombieInfection : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cure-zombie-infection", ("chance", Probability)); 

    // Removes the Zombie Infection Components
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.RemoveComponent<ZombifyOnDeathComponent>(args.SolutionEntity);
        entityManager.RemoveComponent<PendingZombieComponent>(args.SolutionEntity);
    }
}

