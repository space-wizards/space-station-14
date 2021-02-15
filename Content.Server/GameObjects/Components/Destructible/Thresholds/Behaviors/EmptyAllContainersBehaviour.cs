#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;


namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from all containers
    /// </summary>
    public class EmptyAllContainersBehaviour : IThresholdBehavior
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (owner.Deleted || !owner.TryGetComponent<ContainerManagerComponent>(out var containerManager))
                return;

            foreach (var container in containerManager.GetAllContainers())
            {
                container.EmptyContainer(true, owner.Transform.Coordinates);
            }
        }

    }
}
