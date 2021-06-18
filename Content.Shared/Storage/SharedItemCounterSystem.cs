using System.Diagnostics.CodeAnalysis;
using Content.Shared.Tag;
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

            SubscribeLocalEvent<SharedItemCounterComponent, EntInsertedIntoContainerMessage>(HandleEntityInsert);
            SubscribeLocalEvent<SharedItemCounterComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);
        }

        private void HandleEntityRemoved(EntityUid uid, SharedItemCounterComponent counterComponent,
            EntRemovedFromContainerMessage args)
        {
            UpdateSprite(counterComponent, args, false);
        }

        private void HandleEntityInsert(EntityUid uid, SharedItemCounterComponent counterComponent,
            EntInsertedIntoContainerMessage args)
        {
            UpdateSprite(counterComponent, args, true);
        }

        private static void UpdateSprite(SharedItemCounterComponent counterComponent,
            ContainerModifiedMessage args, bool show)
        {
            if (args.Container.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent))
            {
                if (counterComponent.HasItemMap()
                    && counterComponent.TryFindEntity(args.Entity, out var layer))
                {
                    appearanceComponent.SetData(StorageMapHelper.GetVisibleLayer(layer), show);
                }
            }
        }
    }

    public static class StorageMapHelper
    {
        public static string GetVisibleLayer(string layer)
        {
            return $"storage_{layer}_visible";
        }

        private static bool Matches(this IEntity entity, SharedItemCounterComponent.LayerProperties layerProp)
        {
            var entityId = entity.Prototype?.ID;
            if (entityId != null
                && layerProp.Id != null
                && layerProp.Id.Contains(entityId))
            {
                return true;
            }

            return layerProp.Tags != null && entity.HasAnyTag(layerProp.Tags);
        }

        public static bool TryFindEntity(this SharedItemCounterComponent self, IEntity entity,
            [NotNullWhen(true)] out string? layer)
        {
            foreach (var layerProp in self._mapLayers)
            {
                if (entity.Matches(layerProp))
                {
                    layer = layerProp.Layer;
                    return true;
                }
            }

            layer = null;
            return false;
        }

        public static bool HasItemMap(this SharedItemCounterComponent self)
        {
            return self._mapLayers.Count > 0;
        }
    }
}
