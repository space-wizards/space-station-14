#nullable enable

using System.Collections.Generic;
using Content.Shared.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class StackVisualizer : AppearanceVisualizer
    {
        private const int IconLayer = 0;
        private readonly List<string> _opaqueSprites = new();
        private readonly List<string> _transparentSprites = new();

        public override void LoadData(YamlMappingNode mapping)
        {
            base.LoadData(mapping);

            if (mapping.TryGetNode<YamlSequenceNode>("transparentLayers", out var transparentLayers))
            {
                foreach (var yamlNode in transparentLayers)
                {
                    _transparentSprites.Add(((YamlScalarNode) yamlNode).Value!);
                }
            }

            if (mapping.TryGetNode<YamlSequenceNode>("opaqueLayers", out var opaqueLayers))
            {
                foreach (var yamlNode in opaqueLayers)
                {
                    _opaqueSprites.Add(((YamlScalarNode) yamlNode).Value!);
                }
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (_transparentSprites.Count > 0
                && entity.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                foreach (var sprite in _transparentSprites)
                {
                    var rsiPath = spriteComponent.BaseRSI!.Path!;
                    spriteComponent.LayerMapReserveBlank(sprite);
                    spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(rsiPath, sprite));
                    spriteComponent.LayerSetVisible(sprite, false);
                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                if (!component.TryGetData<int>(StackVisuals.Actual, out var actual))
                {
                    return;
                }

                if (!component.TryGetData<int>(StackVisuals.MaxCount, out var maxCount))
                {
                    return;
                }

                if (component.TryGetData<bool>(StackVisuals.Hide, out var hide))
                {
                    if (hide)
                    {
                        foreach (var transparentSprite in _transparentSprites)
                        {
                            spriteComponent.LayerSetVisible(transparentSprite, false);
                        }

                        return;
                    }
                }

                if (_opaqueSprites.Count > 0)
                {
                    var activeLayer = ContentHelpers.RoundToNearestIndex(actual, maxCount, _opaqueSprites.Count);
                    spriteComponent.LayerSetState(IconLayer, _opaqueSprites[activeLayer]);
                }

                var layersSize = _transparentSprites.Count;
                if (layersSize > 0)
                {
                    var activeTill = ContentHelpers.RoundToNearestIndex(actual, maxCount, layersSize + 1);
                    for (var i = 0; i < layersSize; i++)
                    {
                        spriteComponent.LayerSetVisible(_transparentSprites[i], i < activeTill);
                    }
                }
            }
        }
    }
}