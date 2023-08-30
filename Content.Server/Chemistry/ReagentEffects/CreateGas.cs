using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CreateGas : ReagentEffect
{
    [DataField("gas", required: true)]
    public Gas Gas = default!;

    /// <summary>
    ///     For each unit consumed, how many moles of gas should be created?
    /// </summary>
    [DataField("multiplier")]
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

    public override void Effect(ReagentEffectArgs args)
    {
        var atmosSys = args.EntityManager.EntitySysManager.GetEntitySystem<AtmosphereSystem>();

        var tileMix = atmosSys.GetContainingMixture(args.SolutionEntity, false, true);

        if (tileMix != null)
        {
            tileMix.AdjustMoles(Gas, args.Quantity.Float() * Multiplier);
        }
    }
}
