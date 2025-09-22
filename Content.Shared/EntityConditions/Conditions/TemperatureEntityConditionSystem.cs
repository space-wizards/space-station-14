using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Temperature.Components;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TemperatureEntityConditionSystem : EntityConditionSystem<TemperatureComponent, Temperature>
{
    protected override void Condition(Entity<TemperatureComponent> entity, ref EntityConditionEvent<Temperature> args)
    {
        if (entity.Comp.CurrentTemperature >= args.Condition.Min && entity.Comp.CurrentTemperature <= args.Condition.Max)
            args.Result = true;
    }
}

// TODO: These should be merged together when we get a proper temperature struct
public sealed partial class SolutionTemperatureEntityConditionSystem : EntityConditionSystem<SolutionComponent, Temperature>
{
    protected override void Condition(Entity<SolutionComponent> entity, ref EntityConditionEvent<Temperature> args)
    {
        if (entity.Comp.Solution.Temperature >= args.Condition.Min && entity.Comp.Solution.Temperature <= args.Condition.Max)
            args.Result = true;
    }
}

[DataDefinition]
public sealed partial class Temperature : EntityConditionBase<Temperature>
{
    /// <summary>
    /// Minimum allowed temperature
    /// </summary>
    [DataField]
    public float Min = 0;

    /// <summary>
    /// Maximum allowed temperature
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;

    /*
    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-body-temperature",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
    }*/
}
