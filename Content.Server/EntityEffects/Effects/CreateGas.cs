using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class CreateGas : EntityEffect
{
    [DataField(required: true)]
    public Gas Gas = default!;

    /// <summary>
    ///     For each unit consumed, how many moles of gas should be created?
    /// </summary>
    [DataField]
    public float Multiplier = 3f;

    public override bool ShouldLog => true;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var atmos = entSys.GetEntitySystem<AtmosphereSystem>();
        var gasProto = atmos.GetGas(Gas);

        return Loc.GetString("reagent-effect-guidebook-create-gas",
            ("chance", Probability),
            ("moles", Multiplier),
            ("gas", gasProto.Name));
    }

    public override LogImpact LogImpact => LogImpact.High;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var atmosSys = args.EntityManager.EntitySysManager.GetEntitySystem<AtmosphereSystem>();

        var tileMix = atmosSys.GetContainingMixture(args.TargetEntity, false, true);

        if (tileMix != null)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                tileMix.AdjustMoles(Gas, reagentArgs.Quantity.Float() * Multiplier);
            }
            else
            {
                tileMix.AdjustMoles(Gas, Multiplier);
            }
        }
    }
}
