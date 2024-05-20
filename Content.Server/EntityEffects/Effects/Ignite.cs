using Content.Server.Atmos.EntitySystems;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Ignites a mob.
/// </summary>
public sealed partial class Ignite : EntityEffect
{
    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-ignite", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;

    public override void Effect(EntityEffectArgs args)
    {
        var flamSys = EntitySystem.Get<FlammableSystem>();
        flamSys.Ignite(args.TargetEntity, args.OrganEntity ?? args.TargetEntity);
    }
}
