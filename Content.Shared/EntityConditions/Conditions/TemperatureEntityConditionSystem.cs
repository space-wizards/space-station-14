using Content.Shared.Chemistry.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if this entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class TemperatureEntityConditionSystem : EntityConditionSystem<TemperatureComponent, TemperatureCondition>
{
    protected override void Condition(Entity<TemperatureComponent> entity,
        TemperatureCondition condition,
        EntityUid? sourceEnt,
        ref bool result)
    {
        if (entity.Comp.CurrentTemperature >= condition.Min && entity.Comp.CurrentTemperature <= condition.Max)
            result = true;
    }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class SolutionTemperatureEntityConditionSystem : EntityConditionSystem<SolutionComponent, TemperatureCondition>
{
    protected override void Condition(Entity<SolutionComponent> entity,
        TemperatureCondition condition,
        EntityUid? sourceEnt,
        ref bool result)
    {
        if (entity.Comp.Solution.Temperature >= condition.Min && entity.Comp.Solution.Temperature <= condition.Max)
            result = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TemperatureCondition : EntityCondition
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
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
}
