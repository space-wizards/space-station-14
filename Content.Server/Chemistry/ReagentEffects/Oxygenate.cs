using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class Oxygenate : ReagentEffect
{
    [DataField("factor")]
    public float Factor = 1f;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<RespiratorComponent>(args.SolutionEntity, out var resp))
        {
            var respSys = EntitySystem.Get<RespiratorSystem>();
            respSys.UpdateSaturation(resp.Owner, args.Quantity.Float() * Factor, resp);
        }
    }
}
