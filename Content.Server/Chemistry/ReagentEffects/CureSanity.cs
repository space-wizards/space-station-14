using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Content.Shared.Sanity.Components;


namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CureSanity : ReagentEffect
{

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("спасает от импульса обелиска", ("chance", Probability));
    }

    // Removes the Zombie Infection Components
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        if (!entityManager.HasComponent<SanityComponent>(args.SolutionEntity))
            return;

        if (!entityManager.TryGetComponent<SanityComponent>(args.SolutionEntity, out var xform))
            return;

        xform.lvl = 100;
    }
}

