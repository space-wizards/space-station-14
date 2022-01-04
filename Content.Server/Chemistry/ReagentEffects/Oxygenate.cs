using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects;

public class Oxygenate : ReagentEffect
{
    [DataField("factor")]
    public float Factor = 1f;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<SharedMechanismComponent>(args.SolutionEntity, out var mech))
        {
            if (mech.Body == null) return;
            if (args.EntityManager.TryGetComponent<RespiratorComponent>(mech.Body.Owner, out var respirator))
            {
                respirator.Saturation += args.Quantity.Float() * Factor;
            }
        }
    }
}
