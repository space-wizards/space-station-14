using Content.Server.Zombies;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class CauseZombieInfection : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-zombie-infection", ("chance", Probability));

    // Adds the Zombie Infection Components
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.EnsureComponent<ZombifyOnDeathComponent>(args.TargetEntity);
        entityManager.EnsureComponent<PendingZombieComponent>(args.TargetEntity);
    }
}

