using Content.Server.GameTicking.Rules;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

using Robust.Shared.Configuration;
using Content.Server.Zombies;
using Content.Shared.Zombies;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed class CauseZombieInfection : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-zombie-infection", ("chance", Probability));

    // Adds the Zombie Infection Components
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.System<ZombieRuleSystem>().InfectPlayer(args.SolutionEntity);
    }
}

