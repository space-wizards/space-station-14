using Content.Server.Emp;
using Content.Shared.EntityEffects;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;


[DataDefinition]
public sealed partial class EmpReactionEffect : EntityEffect
{
    /// <summary>
    ///     Impulse range per unit of quantity
    /// </summary>
    [DataField("rangePerUnit")]
    public float EmpRangePerUnit = 0.5f;

    /// <summary>
    ///     Maximum impulse range
    /// </summary>
    [DataField("maxRange")]
    public float EmpMaxRange = 10;

    /// <summary>
    ///     How much energy will be drain from sources
    /// </summary>
    [DataField]
    public float EnergyConsumption = 12500;

    /// <summary>
    ///     Amount of time entities will be disabled
    /// </summary>
    [DataField("duration")]
    public float DisableDuration = 15;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-emp-reaction-effect", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var tSys = args.EntityManager.System<TransformSystem>();
        var transform = args.EntityManager.GetComponent<TransformComponent>(args.TargetEntity);

        var range = EmpRangePerUnit;

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            range = MathF.Min((float) (reagentArgs.Quantity * EmpRangePerUnit), EmpMaxRange);
        }

        args.EntityManager.System<EmpSystem>()
            .EmpPulse(tSys.GetMapCoordinates(args.TargetEntity, xform: transform),
            range,
            EnergyConsumption,
            DisableDuration);
    }
}
