using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class ModifyBloodLevel : ReagentEffect
{
    [DataField("scaled")]
    public bool Scaled = false;

    [DataField("amount")]
    public FixedPoint2 Amount = 1.0f;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.SolutionEntity, out var blood))
        {
            var sys = EntitySystem.Get<BloodstreamSystem>();
            var amt = Scaled ? Amount * args.Quantity : Amount;
            sys.TryModifyBloodLevel(args.SolutionEntity, amt, blood);
        }
    }
}
