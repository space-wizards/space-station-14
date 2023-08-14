using Content.Shared.Random.Helpers;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from specified containers
    /// </summary>
    [DataDefinition]
    public sealed class EmptyContainersBehaviour : IThresholdBehavior
    {
        [DataField("containers")]
        public List<string> Containers = new();

        [DataField("randomOffset")]
        public float RandomOffset = 0.25f;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            if (!system.EntityManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            var containerSys = system.EntityManager.System<ContainerSystem>();


            foreach (var containerId in Containers)
            {
                if (!containerSys.TryGetContainer(owner, containerId, out var container, containerManager))
                    continue;

                var entities = containerSys.EmptyContainer(container, true);
                foreach (var ent in entities)
                {
                    ent.RandomOffset(RandomOffset);
                }
            }
        }
    }
}
