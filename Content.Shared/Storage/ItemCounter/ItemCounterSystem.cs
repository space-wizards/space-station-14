using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.ItemCounter
{
    [UsedImplicitly]
    public abstract class ItemCounterSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ItemCounterComponent, EntInsertedIntoContainerMessage>(HandleEntityInsert);
            SubscribeLocalEvent<ItemCounterComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);
        }

        private void HandleEntityRemoved(EntityUid uid, ItemCounterComponent _,
            EntRemovedFromContainerMessage args)
        {
            if (TryGetContainer(args, out var containerEntity, out var containedEntities))
            {
                UpdateSprite(containerEntity, containedEntities);
            }
        }

        private void HandleEntityInsert(EntityUid uid, ItemCounterComponent _,
            EntInsertedIntoContainerMessage args)
        {
            if (TryGetContainer(args, out var containerEntity, out var containedEntities))
            {
                UpdateSprite(containerEntity, containedEntities);
            }
        }

        private void UpdateSprite(IEntity containerEntity, IReadOnlyList<EntityUid> uids)
        {
            if (containerEntity.TryGetComponent(out SharedAppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowEntityData(uids));
            }
        }

        protected abstract bool TryGetContainer(ContainerModifiedMessage msg,
            [NotNullWhen(true)] out IEntity? containerEntity, 
            out IReadOnlyList<EntityUid> containedEntities);
    }
}