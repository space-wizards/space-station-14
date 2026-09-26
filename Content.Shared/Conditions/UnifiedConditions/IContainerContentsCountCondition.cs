using Robust.Shared.Containers;

namespace Content.Shared.Conditions.UnifiedConditions;
/// <summary>
/// A condition that evaluates to the number of contents inside a container.
/// </summary>
public interface IContainerContentsCountCondition : ICondition<IContainerContentsCountCondition>
{
    string Container { get; }
}

public sealed partial class ContainerContentsCountConditionSystem : EntitySystem
{

    [Dependency] private SharedContainerSystem _containerSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<ContainerManagerComponent> entity, ref ConditionEvaluationEvent<IContainerContentsCountCondition> args)
    {
        args.Handled = true;

        if (!_containerSystem.TryGetContainer(args.EntityUid, args.Condition.Container, out var container))
            return;
        args.Value = container.ContainedEntities.Count;
    }
}
