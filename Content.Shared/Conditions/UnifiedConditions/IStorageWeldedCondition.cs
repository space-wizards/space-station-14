using Content.Shared.Tools.Systems;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IStorageWeldedCondition : ICondition
{
    bool Welded { get; }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class StorageWeldedConditionSystem : ConditionEvaluatorSystem<IStorageWeldedCondition>
{
    [Dependency] private WeldableSystem _weldableSystem = default!;

    public override float Evaluate(IStorageWeldedCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return _weldableSystem.IsWelded(entityUid) ? 1 : 0;
    }
}
