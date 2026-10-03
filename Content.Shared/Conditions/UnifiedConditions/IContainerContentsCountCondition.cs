using Robust.Shared.Containers;

namespace Content.Shared.Conditions.UnifiedConditions;

/// <summary>
/// A condition that evaluates to the number of contents inside a container.
/// </summary>
public interface IContainerContentsCountCondition : ICondition
{
    string Container { get; }
}

public sealed partial class ContainerContentsCountConditionSystem : ConditionEvaluatorSystem<IContainerContentsCountCondition>
{
    [Dependency] private SharedContainerSystem _containerSystem = default!;

    public override float Evaluate(IContainerContentsCountCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!_containerSystem.TryGetContainer(entityUid, condition.Container, out var container))
            return 0;
        return container.ContainedEntities.Count;
    }
}
