using Content.Shared.Conditions;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// A condition which passes if the specified <see cref="SatiationTypePrototype"/> is between the specified
/// <see cref="SatiationCondition.Min"/> and <see cref="SatiationCondition.Max"/>. If the entity does not have the
/// specified satiation, the condition evaluates to false.
/// </summary>
public sealed partial class SatiationEntityConditionSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;

    [SubscribeLocalEvent]
   private void Condition(Entity<SatiationComponent> entity,
        ref ConditionEvaluationEvent<SatiationCondition> args)
   {
       args.Handled = true;

        if (_satiation.GetValueOrNull(entity, args.Condition.SatiationType) is not { } satiation)
            return;

        args.Value = (satiation - args.Condition.Min) / (args.Condition.Max - args.Condition.Min);

    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class SatiationCondition : EntityConditionBase<SatiationCondition>, IWithBoundary
{
    /// <summary>
    /// The value above which this condition will fail. If <see cref="MaxInclusive"/> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;

    /// <summary>
    /// The value below which this condition will fail. If <see cref="MinInclusive"/> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    [DataField]
    public float Min = 0;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Max"/> will NOT fail.
    /// </summary>
    [DataField]
    public bool MaxInclusive = false;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Min"/> will NOT fail.
    /// </summary>
    [DataField]
    public bool MinInclusive = false;

    /// <summary>
    /// The type of satiation whose value will be considered.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> SatiationType;

    /// <inheritdoc/>
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-total-satiation",
            ("max", float.IsPositiveInfinity(Max) ? int.MaxValue : Max),
            ("min", Min),
            ("type", prototype.Index(SatiationType).Name));
    }

    public float LowerBound => 0;

    public bool IncludeLowerBound => MinInclusive;

    public float UpperBound => 1;

    public bool IncludeUpperBound => MaxInclusive;

}
