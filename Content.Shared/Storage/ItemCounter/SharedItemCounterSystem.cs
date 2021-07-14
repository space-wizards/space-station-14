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
            SubscribeLocalEvent<ItemCounterComponent, ComponentInit>(InitLayers);
            SubscribeLocalEvent<ItemCounterComponent, EntInsertedIntoContainerMessage>(HandleEntityInsert);
            SubscribeLocalEvent<ItemCounterComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);
        }

        private void InitLayers(EntityUid uid, ItemCounterComponent component, ComponentInit args)
        {
            if (component.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent))
            {
                var list = new List<string>(component.MapLayers.Keys);
                appearanceComponent.SetData(StorageMapVisuals.InitLayers, new ShowLayerData(list));
            }
        }

        private void HandleEntityRemoved(EntityUid uid, ItemCounterComponent itemCounter,
            EntRemovedFromContainerMessage args)
        {
            if (itemCounter.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)
                && TryGetContainer(args, itemCounter, out var containedLayers))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers));
            }
        }

        private void HandleEntityInsert(EntityUid uid, ItemCounterComponent itemCounter,
            EntInsertedIntoContainerMessage args)
        {
            if (itemCounter.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)
                && TryGetContainer(args, itemCounter, out var containedLayers))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers));
            }
        }

        protected abstract bool TryGetContainer(ContainerModifiedMessage msg,
            ItemCounterComponent itemCounter,
            out IReadOnlyList<string> containedLayers);
    }
}