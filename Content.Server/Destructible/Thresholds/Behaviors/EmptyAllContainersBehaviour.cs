using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from all containers
    /// </summary>
    [DataDefinition]
    public sealed partial class EmptyAllContainersBehaviour : IThresholdBehavior
    {
        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            if (!entManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            var containerSys = entManager.System<SharedContainerSystem>();

            foreach (var container in containerManager.GetAllContainers())
            {
                containerSys.EmptyContainer(container, true, entManager.GetComponent<TransformComponent>(owner).Coordinates);
            }
        }
    }
}
