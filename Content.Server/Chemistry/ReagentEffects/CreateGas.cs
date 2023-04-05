using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class CreateGas : ReagentEffect
{
    [DataField("gas", required: true)]
    public Gas Gas = default!;

    /// <summary>
    ///     For each unit consumed, how many moles of gas should be created?
    /// </summary>
    [DataField("multiplier")]
    public float Multiplier = 3f;

    public override bool ShouldLog => true;
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
