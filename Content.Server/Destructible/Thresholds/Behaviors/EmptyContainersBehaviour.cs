using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors;


/// <summary>
///     Drop all items from specified containers
/// </summary>
[DataDefinition]
public sealed partial class EmptyContainersBehaviour : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly ContainerSystem _container = default!;

    [DataField]
    public List<string> Containers = new();

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        if (!TryComp<ContainerManagerComponent>(owner, out var containerManager))
            return;

        foreach (var containerId in Containers)
        {
            if (!_container.TryGetContainer(owner, containerId, out var container, containerManager))
                continue;

            _container.EmptyContainer(container, true);
        }
    }
}

