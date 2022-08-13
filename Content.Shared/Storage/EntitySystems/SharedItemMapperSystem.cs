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
        protected abstract bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> containedLayers);
    }
}
