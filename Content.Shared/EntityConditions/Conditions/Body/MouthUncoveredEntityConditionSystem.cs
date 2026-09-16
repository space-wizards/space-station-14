using Content.Shared.Conditions;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// A condition which passes if the specified entity has their mouth uncovered, generally meaning they're able to eat or drink.
/// </summary>
public sealed partial class MouthUncoveredEntityConditionSystem : EntitySystem
{
    [Dependency] private IngestionSystem _ingestion = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<InventoryComponent> entity, ref ConditionEvaluationEvent<MouthUncoveredCondition> args)
    {
        args.Value = _ingestion.HasMouthAvailable(entity, args.Condition.Slots)?1:0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MouthUncoveredCondition : EntityConditionBase<MouthUncoveredCondition>
{
    /// <summary>
    /// The slots to check for <see cref="IngestionBlockerComponent"/>.
    /// </summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.HEAD | SlotFlags.MASK;

    /// <inheritdoc/>
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("entity-condition-guidebook-mouth-uncovered-condition");
    }
}
