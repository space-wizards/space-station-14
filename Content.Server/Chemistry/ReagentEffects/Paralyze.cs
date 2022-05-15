using Content.Shared.Chemistry.Reagent;
using Content.Server.Stunnable;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class Paralyze : ReagentEffect
{
    [DataField("paralyzeTime")] public double ParalyzeTime = 2;

    /// <remarks>
    ///     true - refresh paralyze time,  false - accumulate paralyze time
    /// </remarks>
    [DataField("refresh")] public bool Refresh = true;

    public override void Effect(ReagentEffectArgs args)
    {
        EntitySystem.Get<StunSystem>().TryParalyze(args.SolutionEntity, TimeSpan.FromSeconds(ParalyzeTime), Refresh);
    }
}

