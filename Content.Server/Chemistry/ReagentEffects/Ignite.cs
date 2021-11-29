using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
///     Ignites a mob.
/// </summary>
public class Ignite : ReagentEffect
{
    public override bool ShouldLog => true;
    public override LogImpact LogImpact => LogImpact.Medium;

    public override void Effect(ReagentEffectArgs args)
    {
        var flamSys = EntitySystem.Get<FlammableSystem>();
        flamSys.Ignite(args.SolutionEntity);
    }
}
