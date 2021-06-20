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
            SubscribeNetworkEvent<AfterStorageFillEvent>(OnStorageFill);
            SubscribeLocalEvent<SharedItemCounterComponent, EntInsertedIntoContainerMessage>(HandleEntityInsert);
            SubscribeLocalEvent<SharedItemCounterComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);
        }

        private void OnStorageFill(AfterStorageFillEvent args)
        {
            if (ComponentManager.TryGetComponent(args.Entity.Uid, out SharedAppearanceComponent appearance))
            {
                var allLayer = new List<EntityUid>();
                foreach (var entity in args.ContainedEntities)
                {
                    allLayer.Add(entity.Uid);
                }

                appearance.SetData(StorageMapVisuals.AllLayers, new ListOfUids(allLayer));
            }
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
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowEntityData(args.Entity.Uid, show));
                // Since visualizers can't be removed we just reset this key to empty value
                // otherwise it would always check on each update
                appearanceComponent.SetData(StorageMapVisuals.AllLayers, new ListOfUids(new List<EntityUid>()));
            }
        }
    }
}
