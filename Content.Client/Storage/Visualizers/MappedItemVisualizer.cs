using System.Collections.Generic;
using System.Linq;
using Content.Shared.Storage;
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
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var spriteComponent)
                && _spriteLayers.Count > 0)
            {
                UpdateAllContainedEntities(component, spriteComponent);
                if (component.TryGetData(StorageMapVisuals.LayerChanged, out ShowEntityData layerData))
                {
                    foreach (var (layerName, layerFilter) in _spriteLayers)
                    {
                        if (component.Owner.EntityManager.TryGetEntity(layerData.Uid, out var entity)
                            && Matches(entity, layerFilter))
                        {
                            spriteComponent.LayerSetVisible(layerName, layerData.Show);
                        }
                    }
                }
            }
        }

        private void UpdateAllContainedEntities(AppearanceComponent component, ISpriteComponent spriteComponent)
        {
            if (component.TryGetData(StorageMapVisuals.AllLayers, out ListOfUids listOfUids)
                && listOfUids.ContainedEntities.Count > 0)
            {
                foreach (var entityUid in listOfUids.ContainedEntities)
                {
                    foreach (var (layerName, layerFilter) in _spriteLayers)
                    {
                        if (component.Owner.EntityManager.TryGetEntity(entityUid, out var entity)
                            && Matches(entity, layerFilter))
                        {
                            spriteComponent.LayerSetVisible(layerName, true);
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
