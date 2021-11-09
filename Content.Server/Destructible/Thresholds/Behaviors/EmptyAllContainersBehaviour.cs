using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from all containers
    /// </summary>
    [DataDefinition]
    public class EmptyAllContainersBehaviour : IThresholdBehavior
    {
        public void Execute(EntityUid owner, DestructibleSystem system, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            foreach (var container in containerManager.GetAllContainers())
            {
                container.EmptyContainer(true, entityManager.GetComponent<TransformComponent>(owner).Coordinates);
            }
        }
    }
}
