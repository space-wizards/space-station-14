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
        private readonly List<string> _spriteLayers = new();
        private bool _isTransparent;

        public override void LoadData(YamlMappingNode mapping)
        {
            base.LoadData(mapping);

            if (mapping.TryGetNode<YamlSequenceNode>("stackLayers", out var spriteSequenceNode))
            {
                foreach (var yamlNode in spriteSequenceNode)
                {
                    _spriteLayers.Add(((YamlScalarNode) yamlNode).Value!);
                }
            }

            if (mapping.TryGetNode<YamlScalarNode>("transparent", out var transparent))
            {
                _isTransparent = transparent.AsBool();
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (_isTransparent
                && _spriteLayers.Count > 0
                && entity.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                foreach (var sprite in _spriteLayers)
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

            if (component.Owner.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                if (_isTransparent)
                {
                    ProcessTransparentSprites(component, spriteComponent);
                }
                else
                {
                    ProcessOpaqueSprites(component, spriteComponent);
                }
            }
        }

        private void ProcessOpaqueSprites(AppearanceComponent component, ISpriteComponent spriteComponent)
        {
            // Skip processing if no actual/maxCount
            if (!component.TryGetData<int>(StackVisuals.Actual, out var actual)) return;
            if (!component.TryGetData<int>(StackVisuals.MaxCount, out var maxCount)) return;

            var activeLayer = ContentHelpers.RoundToNearestIndex(actual, maxCount, _spriteLayers.Count);
            spriteComponent.LayerSetState(IconLayer, _spriteLayers[activeLayer]);
        }

        private void ProcessTransparentSprites(AppearanceComponent component, ISpriteComponent spriteComponent)
        {
            // If hidden, don't render any sprites
            if (!component.TryGetData<bool>(StackVisuals.Hide, out var hide)
                || hide)
            {
                foreach (var transparentSprite in _spriteLayers)
                {
                    spriteComponent.LayerSetVisible(transparentSprite, false);
                }

                return;
            }

            // Skip processing if no actual/maxCount
            if (!component.TryGetData<int>(StackVisuals.Actual, out var actual)) return;
            if (!component.TryGetData<int>(StackVisuals.MaxCount, out var maxCount)) return;

            var activeTill = ContentHelpers.RoundToNearestIndex(actual, maxCount, _spriteLayers.Count + 1);
            for (var i = 0; i < _spriteLayers.Count; i++)
            {
                spriteComponent.LayerSetVisible(_spriteLayers[i], i < activeTill);
            }
        }
    }
}