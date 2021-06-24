using System.Collections.Generic;
using Content.Shared.Storage.ItemCounter;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public class MappedItemVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        private Dictionary<string, SharedMapLayerData> _spriteLayers = new();
        [DataField("sprite")] private ResourcePath? _rsiPath;
        [DataField("mapLayers")] private List<SharedMapLayerData> _mapLayers = new();

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (entity.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                _rsiPath ??= spriteComponent.BaseRSI!.Path!;

                foreach (var (sprite, _) in _spriteLayers)
                {
                    spriteComponent.LayerMapReserveBlank(sprite);
                    spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(_rsiPath, sprite));
                    spriteComponent.LayerSetVisible(sprite, false);
                }
            }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            if (_mapLayers is { Count: > 0 })
            {
                foreach (var layerProp in _mapLayers)
                {
                    if (!_spriteLayers.TryAdd(layerProp.Layer, layerProp))
                        Logger.Warning($"Already added mapLayer with layer = `${layerProp.Layer}` skipping over");
                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var spriteComponent)
                && _spriteLayers.Count > 0)
            {
                if (component.TryGetData(StorageMapVisuals.LayerChanged, out ShowEntityData layerData))
                {
                    foreach (var (entityUid, show) in layerData.QueuedEntities)
                    {
                        foreach (var (layerName, layerFilter) in _spriteLayers)
                        {
                            if (component.Owner.EntityManager.TryGetEntity(entityUid, out var entity)
                                && layerFilter.Whitelist.IsValid(entity))
                            {
                                spriteComponent.LayerSetVisible(layerName, show);
                            }
                        }
                    }

                    layerData.QueuedEntities.Clear();
                }
            }
        }
    }
}
