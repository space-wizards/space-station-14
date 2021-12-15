using System.Collections.Generic;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Storage.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedItemMapperSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ItemMapperComponent, ComponentInit>(InitLayers);
            SubscribeLocalEvent<ItemMapperComponent, EntInsertedIntoContainerMessage>(MapperEntityInserted);
            SubscribeLocalEvent<ItemMapperComponent, EntRemovedFromContainerMessage>(MapperEntityRemoved);
        }

        private void InitLayers(EntityUid uid, ItemMapperComponent component, ComponentInit args)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent))
            {
                var list = new List<string>(component.MapLayers.Keys);
                appearanceComponent.SetData(StorageMapVisuals.InitLayers, new ShowLayerData(list));
            }
        }

        private void MapperEntityRemoved(EntityUid uid, ItemMapperComponent itemMapper,
            EntRemovedFromContainerMessage args)
        {
            if (EntityManager.TryGetComponent(itemMapper.Owner, out AppearanceComponent? appearanceComponent)
                && TryGetLayers(args, itemMapper, out var containedLayers))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers));
            }
        }

        private void MapperEntityInserted(EntityUid uid, ItemMapperComponent itemMapper,
            EntInsertedIntoContainerMessage args)
        {
            if (EntityManager.TryGetComponent(itemMapper.Owner, out AppearanceComponent? appearanceComponent)
                && TryGetLayers(args, itemMapper, out var containedLayers))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers));
            }
        }

        protected abstract bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> containedLayers);
    }
}
