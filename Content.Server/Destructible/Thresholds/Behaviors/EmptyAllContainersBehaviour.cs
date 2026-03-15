using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Drop all items from all containers
/// </summary>
[DataDefinition]
public sealed partial class EmptyAllContainersBehaviour : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        if (!TryComp<ContainerManagerComponent>(owner, out var containerManager))
            return;

        foreach (var container in _container.GetAllContainers(owner, containerManager))
        {
            _container.EmptyContainer(container, true, Transform(owner).Coordinates);
        }
    }
}
