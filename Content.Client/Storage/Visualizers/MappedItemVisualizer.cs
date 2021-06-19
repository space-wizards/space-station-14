using System.Collections.Generic;
using Content.Shared.Storage;
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
        [DataField("sprite")] private ResourcePath? _spritePath;
        [DataField("mapLayers")] private List<SharedMapLayerData>? _mapLayers;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (entity.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                _spritePath ??= spriteComponent.BaseRSI!.Path!;

                foreach (var (sprite,_) in _spriteLayers)
                {
                    spriteComponent.LayerMapReserveBlank(sprite);
                    spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(_spritePath!, sprite));
                    spriteComponent.LayerSetVisible(sprite, false);
                }
            }
        }

        public void AfterDeserialization()
        {
            if (_mapLayers is { Count: > 0 })
            {
                foreach (var layerProp in _mapLayers)
                {
                    if (_spriteLayers.ContainsKey(layerProp.Layer))
                    {
                        Logger.Warning($"Already added mapLayer with layer = `${layerProp.Layer}` skipping over");
                    }
                    else
                    {
                        _spriteLayers.Add(layerProp.Layer, layerProp);
                    }


                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                if (_spriteLayers.Count > 0)
                {
                    foreach (var (layerName, layerFilter) in _spriteLayers)
                    {
                        if (component.TryGetData<ShowEntityData>(StorageMapVisuals.LayerChanged, out var layerData)
                            && component.Owner.EntityManager.TryGetEntity(layerData.Uid, out var entity)
                            && Matches(entity, layerFilter))
                        {
                            spriteComponent.LayerSetVisible(layerName, layerData.Show);
                        }
                    }
                }
            }
        }

        private bool Matches(IEntity entity, SharedMapLayerData layerFilter)
        {
            if (entity.Prototype != null && entity.Prototype.ID.Equals(layerFilter.Id))
            {
                return true;
            }

            return layerFilter.Whitelist != null && layerFilter.Whitelist.IsValid(entity);
        }
    }
}
