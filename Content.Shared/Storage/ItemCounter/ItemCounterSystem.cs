using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.ItemCounter
{
    [UsedImplicitly]
    public class ItemCounterSystem : EntitySystem
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
            UpdateSprite(args, false);
        }

        private void HandleEntityInsert(EntityUid uid, ItemCounterComponent _,
            EntInsertedIntoContainerMessage args)
        {
            UpdateSprite(args, true);
        }

        private static void UpdateSprite(ContainerModifiedMessage args, bool show)
        {
            if (args.Container.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent))
            {
                var newData = new ShowEntityData();
                if (appearanceComponent.TryGetData(StorageMapVisuals.LayerChanged, out ShowEntityData oldData))
                {
                    newData = new(oldData);
                }

                newData.QueuedEntities.Add((args.Entity.Uid, show));
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, newData);
            }
        }
    }
}