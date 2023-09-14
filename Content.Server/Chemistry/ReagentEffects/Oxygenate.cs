using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class Oxygenate : ReagentEffect
{
    [DataField("factor")]
    public float Factor = 1f;

    // JUSTIFICATION: This is internal magic that players never directly interact with.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<RespiratorComponent>(args.SolutionEntity, out var resp))
        {
            var respSys = EntitySystem.Get<RespiratorSystem>();
            respSys.UpdateSaturation(resp.Owner, args.Quantity.Float() * Factor, resp);
        }
    }
}
