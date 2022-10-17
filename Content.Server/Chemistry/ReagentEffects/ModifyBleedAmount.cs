using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class ModifyBleedAmount : ReagentEffect
{
    [DataField("scaled")]
    public bool Scaled = false;

    [DataField("amount")]
    public float Amount = -1.0f;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.SolutionEntity, out var blood))
        {
            var sys = EntitySystem.Get<BloodstreamSystem>();
            var amt = Scaled ? Amount * args.Quantity.Float() : Amount;
            if (args.MetabolismEffects != null)
                amt *= (float) (args.Quantity / args.MetabolismEffects.MetabolismRate);

            sys.TryModifyBleedAmount(args.SolutionEntity, amt, blood);
        }
    }
}
