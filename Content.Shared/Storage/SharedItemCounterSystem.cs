using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage
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
            if (ComponentManager.HasComponent<SharedItemCounterComponent>(args.Entity.Uid)
                && ComponentManager.TryGetComponent(args.Entity.Uid, out SharedAppearanceComponent appearance)
                && args.ContainedEntities != null)
            {
                var allLayer = new List<ShowEntityData>();
                foreach (var entity in args.ContainedEntities)
                {
                    allLayer.Add(new ShowEntityData(entity.Uid, true));
                }

                appearance.SetData(StorageMapVisuals.AllLayers, allLayer);
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
            }
        }
    }
}
