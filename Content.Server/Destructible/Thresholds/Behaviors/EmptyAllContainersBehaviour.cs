using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Drop all items from all containers
/// </summary>
[DataDefinition]
public sealed partial class EmptyAllContainersBehaviour : IThresholdBehavior
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        if (!system.EntityManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
            return;

        foreach (var container in _container.GetAllContainers(owner, containerManager))
        {
            _container.EmptyContainer(container, true, system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates);
        }
    }
}
