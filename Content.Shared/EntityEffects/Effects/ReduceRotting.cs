using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Atmos.Rotting;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Reduces the rotting accumulator on the patient, making them revivable.
/// </summary>
public sealed partial class ReduceRotting : EntityEffect
{
    [DataField("seconds")]
    public double RottingAmount = 10;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reduce-rotting",
            ("chance", Probability),
            ("time", RottingAmount));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Scale != 1f)
                return;
        }

        var rottingSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRottingSystem>();

        rottingSys.ReduceAccumulator(args.TargetEntity, TimeSpan.FromSeconds(RottingAmount));
    }
}
