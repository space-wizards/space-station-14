using Content.Server.Body.Components;
using Content.Shared._Offbrand.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Server._Offbrand.EntityEffects;

public sealed class MetaboliteThresholdSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CheckEntityEffectConditionEvent<MetaboliteThreshold>>(OnCheckMetaboliteThreshold);
    }

    private void OnCheckMetaboliteThreshold(ref CheckEntityEffectConditionEvent<MetaboliteThreshold> args)
    {
        if (args.Args is not EntityEffectReagentArgs reagentArgs)
            throw new NotImplementedException();

        var reagent = args.Condition.Reagent;
        if (reagent == null)
            reagent = reagentArgs.Reagent?.ID;

        if (reagent is not { } metaboliteReagent)
        {
            args.Result = true;
            return;
        }

        if (!TryComp<MetabolizerComponent>(reagentArgs.OrganEntity, out var metabolizer))
        {
            args.Result = true;
            return;
        }

        var metabolites = metabolizer.Metabolites;

        var quant = FixedPoint2.Zero;
        metabolites.TryGetValue(metaboliteReagent, out quant);

        if (args.Condition.IncludeBloodstream && reagentArgs.Source != null)
        {
            quant += reagentArgs.Source.GetTotalPrototypeQuantity(metaboliteReagent);
        }

        args.Result = quant >= args.Condition.Min && quant <= args.Condition.Max;
    }
}
