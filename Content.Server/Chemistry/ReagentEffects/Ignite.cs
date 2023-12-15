using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Ignites a mob.
/// </summary>
public sealed partial class Ignite : ReagentEffect
{
    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-ignite", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;

    public override void Effect(ReagentEffectArgs args)
    {
        var flamSys = EntitySystem.Get<FlammableSystem>();
        flamSys.Ignite(args.SolutionEntity, args.OrganEntity ?? args.SolutionEntity);
    }
}
