using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Content.Server.Zombies;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed class CureZombieInfection : ReagentEffect
{
    [DataField("innoculate")]
    public bool innoculate = false;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if(innoculate == true)
            return Loc.GetString("reagent-effect-guidebook-innoculate-zombie-infection", ("chance", Probability)); 
        
        return Loc.GetString("reagent-effect-guidebook-cure-zombie-infection", ("chance", Probability)); 
    }

    // Removes the Zombie Infection Components
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.RemoveComponent<ZombifyOnDeathComponent>(args.SolutionEntity);
        entityManager.RemoveComponent<PendingZombieComponent>(args.SolutionEntity);

        if (innoculate == true) {
            entityManager.EnsureComponent<ZombieImmuneComponent>(args.SolutionEntity);
        }
    }
}

