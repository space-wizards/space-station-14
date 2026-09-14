using Content.Shared.Chemistry.Components;
using Content.Shared.Conditions;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Evaluates <see cref="TemperatureCondition"/>
/// </summary>
public sealed partial class TemperatureEntityConditionSystem : EntitySystem
{
    /// <summary>
    /// Returns the proportional value of components temperature to the conditions bound.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void Condition(Entity<TemperatureComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not TemperatureCondition condition)
            return;

        args.Value = (entity.Comp.Temperature - condition.Min) / (condition.Max - condition.Min);

        args.Handled = true;
    }

    /// <summary>
    /// Returns the proportional value of components solutions temperature to the conditions bound.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void Condition(Entity<SolutionComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Condition is not TemperatureCondition condition)
            return;

        args.Value = (entity.Comp.Solution.Temperature - condition.Min) / (condition.Max - condition.Min);

        args.Handled = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TemperatureCondition : EntityConditionBase<TemperatureCondition>, IConditionWithBoundary
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

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-body-temperature",
            ("max", float.IsPositiveInfinity(Max) ? (float)int.MaxValue : Max),
            ("min", Min));

    float IConditionWithBoundary.LowerBound => 0;

    bool IConditionWithBoundary.IncludeLowerBound => true;

    float IConditionWithBoundary.UpperBound => 1;

    bool IConditionWithBoundary.IncludeUpperBound => true;
    /// <summary>
    /// Inverted is done by EntityConditionBase and <see cref="Shared"/>
    /// </summary>
    bool IConditionWithBoundary.Inverted => false;
}
