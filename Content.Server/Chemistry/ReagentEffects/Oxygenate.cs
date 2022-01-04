using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
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
            var respSys = EntitySystem.Get<RespiratorSystem>();
            respSys.UpdateSaturation(mech.Body.Owner, args.Quantity.Float() * Factor);
        }
    }
}
