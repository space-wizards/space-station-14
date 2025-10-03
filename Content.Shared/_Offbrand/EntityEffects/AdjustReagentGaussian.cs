using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class AdjustReagentGaussian : EntityEffect
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public double μ;

    [DataField(required: true)]
    public double σ;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            throw new NotImplementedException();

        if (reagentArgs.Source == null)
            return;

        var timing = IoCManager.Resolve<IGameTiming>();

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)timing.CurTick.Value, args.EntityManager.GetNetEntity(args.TargetEntity).Id });
        var rand = new System.Random(seed);

        var amount = rand.NextGaussian(μ, σ);
        amount *= reagentArgs.Scale.Double();

        if (amount < 0 && reagentArgs.Source.ContainsPrototype(Reagent))
            reagentArgs.Source.RemoveReagent(Reagent, -amount);
        else if (amount > 0)
            reagentArgs.Source.AddReagent(Reagent, amount);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var proto = prototype.Index(Reagent);
        return Loc.GetString("reagent-effect-guidebook-adjust-reagent-gaussian",
            ("chance", Probability),
            ("deltasign", Math.Sign(μ)),
            ("reagent", proto.LocalizedName),
            ("mu", Math.Abs(μ)),
            ("sigma", Math.Abs(σ)));
    }
}
