using Content.Server.Emp;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReactionEffects;


[DataDefinition]
public sealed partial class EmpReactionEffect : ReagentEffect
{
    /// <summary>
    ///     Impulse range per unit of reagent
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

    public override void Effect(ReagentEffectArgs args)
    {
        var transform = args.EntityManager.GetComponent<TransformComponent>(args.SolutionEntity);
        var range = MathF.Min((float) (args.Quantity*EmpRangePerUnit), EmpMaxRange);

        args.EntityManager.System<EmpSystem>().EmpPulse(
            transform.MapPosition,
            range,
            EnergyConsumption,
            DisableDuration);
    }
}
