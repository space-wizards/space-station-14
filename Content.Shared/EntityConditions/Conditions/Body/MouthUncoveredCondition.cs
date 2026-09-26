using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MouthUncoveredCondition : EntityConditionBase<IMouthUncoveredCondition>, IMouthUncoveredCondition
{
    /// <summary>
    /// The slots to check for <see cref="IngestionBlockerComponent"/>.
    /// </summary>
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.HEAD | SlotFlags.MASK;

    /// <inheritdoc/>
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-mouth-uncovered-condition");
    }
}
