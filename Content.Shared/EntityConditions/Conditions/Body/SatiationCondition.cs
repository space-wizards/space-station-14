using Content.Shared.Conditions;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;



/// <inheritdoc cref="EntityCondition"/>
public sealed partial class SatiationCondition : EntityConditionBase<ISatiationCondition>, ISatiationCondition
{
    /// <summary>
    /// The value above which this condition will fail. If <see cref="MaxInclusive"/> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    [DataField]
    public float Max { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// The value below which this condition will fail. If <see cref="MinInclusive"/> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    [DataField]
    public float Min { get; set; } = 0;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Max"/> will NOT fail.
    /// </summary>
    [DataField]
    public bool MaxInclusive { get; set; } = false;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Min"/> will NOT fail.
    /// </summary>
    [DataField]
    public bool MinInclusive { get; set; } = false;

    /// <summary>
    /// The type of satiation whose value will be considered.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> SatiationType { get; set; }

    /// <inheritdoc/>
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-total-satiation",
            ("max", float.IsPositiveInfinity(Max) ? int.MaxValue : Max),
            ("min", Min),
            ("type", prototype.Index(SatiationType).Name));
    }

}
