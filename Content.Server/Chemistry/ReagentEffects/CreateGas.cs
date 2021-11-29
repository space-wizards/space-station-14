using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects;

public class CreateGas : ReagentEffect
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
        var atmosSys = EntitySystem.Get<AtmosphereSystem>();

        var xform = args.EntityManager.GetComponent<TransformComponent>(args.SolutionEntity);
        var tileMix = atmosSys.GetTileMixture(xform.Coordinates);

        if (tileMix != null)
        {
            tileMix.AdjustMoles(Gas, args.Quantity.Float() * Multiplier);
        }
    }
}
