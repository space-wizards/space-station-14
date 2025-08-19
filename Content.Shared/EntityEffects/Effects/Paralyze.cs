using Content.Shared.EntityEffects;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class Paralyze : EntityEffect
{
    [DataField] public double ParalyzeTime = 2;

    /// <remarks>
    ///     true - refresh paralyze time,  false - accumulate paralyze time
    /// </remarks>
    [DataField] public bool Refresh = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-paralyze",
            ("chance", Probability),
            ("time", ParalyzeTime));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var paralyzeTime = ParalyzeTime;

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            paralyzeTime *= (double)reagentArgs.Scale;
        }

        args.EntityManager.System<SharedStunSystem>().TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(paralyzeTime), Refresh);
    }
}

