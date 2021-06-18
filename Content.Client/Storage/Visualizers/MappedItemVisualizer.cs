using System.Collections.Generic;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public class MappedItemVisualizer : AppearanceVisualizer
    {
        [DataField("spriteLayers")] private List<string> _spriteLayers = new();
        [DataField("sprite")] private ResourcePath? _spritePath;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (entity.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                _spritePath ??= spriteComponent.BaseRSI!.Path!;
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            InitializeSpriteMap(component);
            UpdateSprite(component);
        }

        private void InitializeSpriteMap(AppearanceComponent component)
        {
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var spriteComponent)
                && component.TryGetData<List<string>>(StorageMapVisuals.AllLayers, out var initLayers))
            {
                // This should only be initialized once
                if (_spriteLayers.Count > 0)
                {
                    return;
                }

                foreach (var sprite in initLayers)
                {
                    spriteComponent.LayerMapReserveBlank(sprite);
                    spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(_spritePath!, sprite));
                    spriteComponent.LayerSetVisible(sprite, false);
                    _spriteLayers.Add(sprite);
                }
            }
        }

        private void UpdateSprite(AppearanceComponent component)
        {
            if (component.Owner.TryGetComponent<ISpriteComponent>(out var spriteComp)
                && _spriteLayers.Count > 0)
            {
                foreach (var layerName in _spriteLayers)
                {
                    if (component.TryGetData<bool>(StorageMapHelper.GetVisibleLayer(layerName), out var show))
                    {
                        spriteComp.LayerSetVisible(layerName, show);
                    }
                }
            }
        }
    }
}
