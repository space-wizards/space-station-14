using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class ModifyBloodLevel : ReagentEffect
{
    [DataField("scaled")]
    public bool Scaled = false;

    [DataField("amount")]
    public FixedPoint2 Amount = 1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-blood-level", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount.Float())));

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.SolutionEntity, out var blood))
        {
            var sys = EntitySystem.Get<BloodstreamSystem>();
            var amt = Scaled ? Amount * args.Quantity : Amount;
            amt *= args.Scale;

            sys.TryModifyBloodLevel(args.SolutionEntity, amt, blood);
        }
    }
}
