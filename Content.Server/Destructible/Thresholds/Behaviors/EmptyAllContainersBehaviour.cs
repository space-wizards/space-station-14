using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from all containers
    /// </summary>
    [DataDefinition]
    public sealed partial class EmptyAllContainersBehaviour : IThresholdBehavior
    {
        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var entMan = system.EntityManager;
            if (!entMan.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            if (!entMan.TryGetComponent<TransformComponent>(owner, out var transform))
                return;

            var containerSys = system.ContainerSystem;
            var coords = transform.Coordinates;

            var containerEnumerable = containerSys.GetAllContainers(owner, containerManager);
            foreach (var container in containerEnumerable)
            {
                containerSys.EmptyContainer(container, true, coords);
            }
        }
    }
}
