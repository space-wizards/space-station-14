using System.Linq;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedItemMapperSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;

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
            foreach (var (layerName, val) in component.MapLayers)
            {
                val.Layer = layerName;
            }

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

        private bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> showLayers)
        {
            var containedLayers = _container.GetAllContainers(msg.Container.Owner)
                .SelectMany(cont => cont.ContainedEntities).ToArray();

            var list = new List<string>();
            foreach (var mapLayerData in itemMapper.MapLayers.Values)
            {
                var count = containedLayers.Count(uid => mapLayerData.ServerWhitelist.IsValid(uid));
                if (count >= mapLayerData.MinCount && count <= mapLayerData.MaxCount)
                {
                    list.Add(mapLayerData.Layer);
                }
            }

            showLayers = list;
            return true;
        }
    }
}
