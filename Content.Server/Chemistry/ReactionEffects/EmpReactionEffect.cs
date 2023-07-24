using Content.Server.Emp;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReactionEffects;


[DataDefinition]
public sealed class EmpReactionEffect : ReagentEffect
{
    /// <summary>
    ///     EMP explosion range
    /// </summary>
    [DataField("range")]
    public float EmpRange = 1;

    /// <summary>
    ///     How much energy will be drain from sources
    /// </summary>
    [DataField("energyConsumption")]
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
        EntitySystem.Get<EmpSystem>().EmpPulse(
            transform.MapPosition,
            EmpRange,
            EnergyConsumption,
            DisableDuration);
    }
}
