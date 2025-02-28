using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class CreateGas : EntityEffect
{
    [DataField(required: true)]
    public Gas Gas = default!;

    public override bool ShouldLog => true;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var atmos = entSys.GetEntitySystem<AtmosphereSystem>();
        var gasProto = atmos.GetGas(Gas);

        return Loc.GetString("reagent-effect-guidebook-create-gas",
            ("chance", Probability),
            ("moles", FixedPoint2.New(Atmospherics.MolarMassToReagentMultiplier / gasProto.MolarMass)),
            ("gas", gasProto.Name));
    }

    public override LogImpact LogImpact => LogImpact.High;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var atmosSys = args.EntityManager.EntitySysManager.GetEntitySystem<AtmosphereSystem>();

        var tileMix = atmosSys.GetContainingMixture(args.TargetEntity, false, true);

        var multiplier = 1 / (Atmospherics.MolarMassToReagentMultiplier * atmosSys.GetGas(Gas).MolarMass);
        if (tileMix != null)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                tileMix.AdjustMoles(Gas, reagentArgs.Quantity.Float() * multiplier);
            }
            else
            {
                tileMix.AdjustMoles(Gas, multiplier);
            }
        }
    }
}
