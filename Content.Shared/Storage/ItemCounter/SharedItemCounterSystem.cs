using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.ItemCounter
{
    [UsedImplicitly]
    public abstract class SharedItemCounterSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedItemCounterComponent, EntInsertedIntoContainerMessage>(HandleEntityInsert);
            SubscribeLocalEvent<SharedItemCounterComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);
        }



        private void HandleEntityRemoved(EntityUid uid, SharedItemCounterComponent _,
            EntRemovedFromContainerMessage args)
        {
            UpdateSprite(args, false);
        }

        private void HandleEntityInsert(EntityUid uid, SharedItemCounterComponent _,
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
