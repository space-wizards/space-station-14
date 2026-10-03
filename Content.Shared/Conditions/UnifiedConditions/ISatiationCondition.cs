using Content.Shared.Conditions.Satisfier;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface ISatiationCondition : ICondition, IConditionWithDefaultSatisfactionRule
{
    /// <summary>
    /// The value above which this condition will fail. If <see cref="MaxInclusive" /> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    float Max { get; }

    /// <summary>
    /// The value below which this condition will fail. If <see cref="MinInclusive" /> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    float Min { get; }

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Max" /> will NOT fail.
    /// </summary>
    bool MaxInclusive { get; }

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Min" /> will NOT fail.
    /// </summary>
    bool MinInclusive { get; }

    /// <summary>
    /// The type of satiation whose value will be considered.
    /// </summary>
    ProtoId<SatiationTypePrototype> SatiationType { get; }

    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithBoundary
        {
            IncludeLowerBound = MinInclusive,
            IncludeUpperBound = MaxInclusive,
            LowerBound = 0,
            UpperBound = 1,
        };
    }
}

/// <summary>
/// A condition which passes if the specified <see cref="SatiationTypePrototype" /> is between the specified
/// <see cref="ISatiationCondition.Min" /> and <see cref="ISatiationCondition.Max" />. If the entity does not have the
/// specified satiation, the condition evaluates to false.
/// </summary>
public sealed partial class SatiationEntityConditionSystem : ConditionEvaluatorSystem<ISatiationCondition>
{
    [Dependency] private SatiationSystem _satiation = default!;

    public override float Evaluate(ISatiationCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        float canWorkIfNull = ((condition.Max - condition.Min == 0f) && condition.Max == 0) ? 1 : 0;

        if (!TryComp(entityUid, out SatiationComponent? satiationComponent))
            return canWorkIfNull;

        var val = _satiation.GetValueOrNull((entityUid, satiationComponent), condition.SatiationType);
        if (!val.HasValue)
            return canWorkIfNull;

        if (condition.Max - condition.Min == 0)
            return val - condition.Max <= 0.001 ? 1 : 0;

        return (val.Value - condition.Min) / (condition.Max - condition.Min);
    }
}
