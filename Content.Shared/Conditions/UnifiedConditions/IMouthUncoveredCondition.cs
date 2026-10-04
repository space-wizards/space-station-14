using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IMouthUncoveredCondition : ICondition
{
    SlotFlags Slots { get; }
}

/// <summary>
/// A condition which passes if the specified entity has their mouth uncovered, generally meaning they're able to eat or
/// drink.
/// </summary>
public sealed partial class MouthUncoveredEntityConditionSystem : ConditionEvaluatorSystem<IMouthUncoveredCondition>
{
    [Dependency] private IngestionSystem _ingestion = default!;

    public override float Evaluate(IMouthUncoveredCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return _ingestion.HasMouthAvailable(entityUid, condition.Slots) ? 1 : 0;
    }
}
