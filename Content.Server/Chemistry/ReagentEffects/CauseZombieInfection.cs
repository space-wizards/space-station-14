using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

using Robust.Shared.Configuration;
using Content.Server.Zombies;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed class CauseZombieInfection : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-paralyze"); 

    // Removes the Zombie Infection Components
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.AddComponent<ZombifyOnDeathComponent>(args.SolutionEntity);
        entityManager.AddComponent<PendingZombieComponent>(args.SolutionEntity);
    }
}

