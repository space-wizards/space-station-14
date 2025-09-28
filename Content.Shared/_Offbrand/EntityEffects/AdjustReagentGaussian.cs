using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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

        var rand = IoCManager.Resolve<IRobustRandom>();
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
