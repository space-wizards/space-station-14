using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class ModifyBleedAmount : ReagentEffect
{
    [DataField("scaled")]
    public bool Scaled = false;

    [DataField("amount")]
    public float Amount = -1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-bleed-amount", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)));

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.SolutionEntity, out var blood))
        {
            var sys = EntitySystem.Get<BloodstreamSystem>();
            var amt = Scaled ? Amount * args.Quantity.Float() : Amount;
            amt *= args.Scale;

            sys.TryModifyBleedAmount(args.SolutionEntity, amt, blood);
        }
    }
}
