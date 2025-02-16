using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from specified containers
    /// </summary>
    [DataDefinition]
    public sealed partial class EmptyContainersBehaviour : IThresholdBehavior
    {
        [DataField]
        public List<string> Containers = new();

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            if (!entManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            var containerSys = entManager.System<ContainerSystem>();

            foreach (var containerId in Containers)
            {
                if (!containerSys.TryGetContainer(owner, containerId, out var container, containerManager))
                    continue;

                containerSys.EmptyContainer(container, true);
            }
        }
    }
}
