using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IMouthUncoveredCondition : ICondition<IMouthUncoveredCondition>
{
    SlotFlags Slots { get; }
}

/// <summary>
/// A condition which passes if the specified entity has their mouth uncovered, generally meaning they're able to eat or drink.
/// </summary>
public sealed partial class MouthUncoveredEntityConditionSystem : EntitySystem
{
    [Dependency] private IngestionSystem _ingestion = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<InventoryComponent> entity, ref ConditionEvaluationEvent<IMouthUncoveredCondition> args)
    {
        args.Value = _ingestion.HasMouthAvailable(entity, args.Condition.Slots)?1:0;
    }
}
