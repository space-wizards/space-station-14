using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
