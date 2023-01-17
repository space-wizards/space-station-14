using System.Linq;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems
{
    /// <summary>
    /// <c>ItemMapperSystem</c> is a system that on each initialization, insertion, removal of an entity from
    /// given <see cref="ItemMapperComponent"/> (with appropriate storage attached) will check each stored item to see
    /// if its tags/component, and overall quantity match <see cref="ItemMapperComponent.MapLayers"/>.
    /// </summary>
    [UsedImplicitly]
    public abstract class SharedItemMapperSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
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
                _appearance.SetData(component.Owner, StorageMapVisuals.InitLayers, new ShowLayerData(list), appearanceComponent);
            }
        }

        private void MapperEntityRemoved(EntityUid uid, ItemMapperComponent itemMapper,
            EntRemovedFromContainerMessage args)
        {
            if (itemMapper.ContainerWhitelist != null && !itemMapper.ContainerWhitelist.Contains(args.Container.ID))
                return;

            UpdateAppearance(uid, itemMapper, args);
        }

        private void MapperEntityInserted(EntityUid uid, ItemMapperComponent itemMapper,
            EntInsertedIntoContainerMessage args)
        {
            if (itemMapper.ContainerWhitelist != null && !itemMapper.ContainerWhitelist.Contains(args.Container.ID))
                return;

            UpdateAppearance(uid, itemMapper, args);
        }

        private void UpdateAppearance(EntityUid uid, ItemMapperComponent itemMapper, ContainerModifiedMessage message)
        {
            if (EntityManager.TryGetComponent(itemMapper.Owner, out AppearanceComponent? appearanceComponent)
                && TryGetLayers(message, itemMapper, out var containedLayers))
            {
                _appearance.SetData(itemMapper.Owner, StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers), appearanceComponent);
            }
        }

        /// <summary>
        /// Method that iterates over storage of the entity in <paramref name="msg"/> and sets <paramref name="containedLayers"/> according to
        /// <paramref name="itemMapper"/> definition. It will have O(n*m) time behavior (n - number of entities in container, and m - number of
        /// definitions in <paramref name="containedLayers"/>.
        /// </summary>
        /// <param name="msg">event with EntityUid used to search the storage</param>
        /// <param name="itemMapper">component that contains definition used to map <see cref="Content.Shared.Whitelist.EntityWhitelist">whitelist</see> in
        /// <c>mapLayers</c> to string.
        /// </param>
        /// <param name="containedLayers">list of <paramref name="itemMapper"/> layers that should be visible</param>
        /// <returns>false if <c>msg.Container.Owner</c> is not a storage, true otherwise.</returns>
        private bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> showLayers)
        {
            var containedLayers = _container.GetAllContainers(msg.Container.Owner)
                .Where(c => itemMapper.ContainerWhitelist?.Contains(c.ID) ?? true).SelectMany(cont => cont.ContainedEntities).ToArray();

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
