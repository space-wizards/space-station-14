using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// A condition which passes if the specified entity has their mouth uncovered, generally meaning they're able to eat or drink.
/// </summary>
public sealed partial class MouthUncoveredEntityConditionSystem : EntityConditionSystem<InventoryComponent, MouthUncoveredCondition>
{
    [Dependency] private IngestionSystem _ingestion = default!;

    /// <inheritdoc/>
    protected override void Condition(Entity<InventoryComponent> entity, ref EntityConditionEvent<MouthUncoveredCondition> args)
    {
        args.Result = _ingestion.HasMouthAvailable(entity, args.Condition.Slots);
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
