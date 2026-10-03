using Content.Shared.Chemistry.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;



/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TemperatureCondition : EntityConditionBase<ITemperatureCondition>, ITemperatureCondition
{
    /// <summary>
    /// Minimum allowed temperature
    /// </summary>
    [DataField]
    public float Min { get; set; } = 0;

    /// <summary>
    /// Maximum allowed temperature
    /// </summary>
    [DataField]
    public float Max { get; set; } = float.PositiveInfinity;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-body-temperature",
            ("max", float.IsPositiveInfinity(Max) ? (float)int.MaxValue : Max),
            ("min", Min));

}
