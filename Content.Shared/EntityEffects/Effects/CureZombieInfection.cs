using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class CureZombieInfection : EntityEffect
{
    [DataField]
    public bool Innoculate;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if(Innoculate)
            return Loc.GetString("reagent-effect-guidebook-innoculate-zombie-infection", ("chance", Probability));

        return Loc.GetString("reagent-effect-guidebook-cure-zombie-infection", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<CureZombieInfection>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}

