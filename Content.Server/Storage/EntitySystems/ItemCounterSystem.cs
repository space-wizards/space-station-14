using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public class ItemCounterSystem : SharedItemCounterSystem
    {
        protected override int? GetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter)
        {
            if (!msg.Container.Owner.TryGetComponent(out ServerStorageComponent? component)
                || component.StoredEntities == null)
            {
                return null;
            }

            var count = 0;
            foreach (var entity in component.StoredEntities)
            {
                if (itemCounter.Count.IsValid(entity.Uid)) count++;
            }

            return count;
        }
    }
}
