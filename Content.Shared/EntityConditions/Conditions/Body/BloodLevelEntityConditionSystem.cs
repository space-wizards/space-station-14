using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if this entity has at certain percentage of blood.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class BloodLevelEntityConditionSystem : EntityConditionSystem<BloodstreamComponent, BloodLevelCondition>
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    protected override void Condition(Entity<BloodstreamComponent> entity, ref EntityConditionEvent<BloodLevelCondition> args)
    {
        var bloodlevel = _bloodstream.GetBloodLevel(entity.AsNullable());

        args.Result =
            (args.Condition.MinInclusive && bloodlevel >= args.Condition.Min || bloodlevel > args.Condition.Min) &&
            (args.Condition.MaxInclusive && bloodlevel <= args.Condition.Max || bloodlevel < args.Condition.Max);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class BloodLevelCondition : EntityConditionBase<BloodLevelCondition>
{
    /// <summary>
    /// Minimum blood level this entity needs to pass the condition.
    /// Inclusive.
    /// </summary>
    [DataField]
    public float Min = 0.5f;

    /// <summary>
    /// Maximum blood level this entity needs to pass the condition.
    /// Inclusive.
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Max"/> will pass the condition.
    /// </summary>
    [DataField]
    public bool MaxInclusive;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Min"/> will pass the condition.
    /// </summary>
    [DataField]
    public bool MinInclusive;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-bloodlevel",
            ("max", float.IsPositiveInfinity(Max) ? int.MaxValue : Max * 100),
            ("min", Min * 100));
    }
}
