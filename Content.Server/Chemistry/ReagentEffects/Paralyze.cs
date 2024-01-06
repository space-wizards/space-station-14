using Content.Server.Stunnable;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class Paralyze : ReagentEffect
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

    public override void Effect(ReagentEffectArgs args)
    {
        var paralyzeTime = ParalyzeTime;
        paralyzeTime *= args.Scale;

        EntitySystem.Get<StunSystem>().TryParalyze(args.SolutionEntity, TimeSpan.FromSeconds(paralyzeTime), Refresh);
    }
}

