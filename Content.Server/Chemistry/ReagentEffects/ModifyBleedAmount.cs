using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Blood.Systems;
using Robust.Shared.Prototypes;
using BloodstreamComponent = Content.Shared.Medical.Blood.Components.BloodstreamComponent;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class ModifyBleedAmount : ReagentEffect
{
    [DataField]
    public bool Scaled = false;

    [DataField]
    public float Amount = -1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-bleed-amount", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)));


    //TODO: Refactor modify bleed amount in reagent effects
    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.SolutionEntity, out var blood))
        {
            var sys = args.EntityManager.System<BloodstreamSystem>();
            var amt = Scaled ? Amount * args.Quantity.Float() : Amount;
            amt *= args.Scale;
            //sys.TryModifyBleedAmount(args.SolutionEntity, amt, blood);
        }
    }
}
