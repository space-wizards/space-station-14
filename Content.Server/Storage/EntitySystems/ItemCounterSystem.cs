using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public sealed class ItemCounterSystem : SharedItemCounterSystem
    {
        protected override int? GetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter)
        {
            if (!EntityManager.TryGetComponent(msg.Container.Owner, out StorageComponent? component))
            {
                return null;
            }

            var count = 0;
            foreach (var entity in component.Container.ContainedEntities)
            {
                if (itemCounter.Count.IsValid(entity))
                    count++;
            }

            return count;
        }
    }
}
