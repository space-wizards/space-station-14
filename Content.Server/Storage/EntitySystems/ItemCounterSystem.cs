using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public class ItemCounterSystem : SharedItemCounterSystem
    {
        protected override int? GetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter)
        {
            if (!EntityManager.TryGetComponent(msg.Container.Owner, out ServerStorageComponent? component)
                || component.StoredEntities == null)
            {
                return null;
            }

            var count = 0;
            foreach (var entity in component.StoredEntities)
            {
                if (itemCounter.Count.IsValid(entity)) count++;
            }

            return count;
        }
    }
}
