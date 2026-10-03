using Content.Shared.Tools.Systems;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IStorageWeldedCondition : ICondition<IStorageWeldedCondition>
{
    bool Welded { get; }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class StorageWeldedConditionSystem : EntitySystem
{
    [Dependency] private WeldableSystem _weldableSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity, ref ConditionEvaluationEvent<IStorageWeldedCondition> args)
    {
        args.Handled = true;
        args.Value = _weldableSystem.IsWelded(entity) ? 1 : 0;
    }
}
