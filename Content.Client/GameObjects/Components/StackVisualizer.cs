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
        private readonly List<string> _spriteStates = new();
        private bool _isOverlay;

        public override void LoadData(YamlMappingNode mapping)
        {
            base.LoadData(mapping);


            if (mapping.TryGetNode("overlay", out YamlNode? overlay))
            {
                _isOverlay = overlay.AsBool();
            }

            if (!mapping.TryGetNode("spritestates", out YamlSequenceNode? stepsMapping))
            {
                return;
            }


            foreach (var yamlNode in stepsMapping)
            {
                var spriteState = ((YamlScalarNode) yamlNode).Value;
                _spriteStates.Add(spriteState!);
            }

        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (_isOverlay
                && entity.TryGetComponent<ISpriteComponent>(out var spriteComponent))
            {
                foreach (var sprite in _spriteStates)
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

                int roundToLevels = ContentHelpers.RoundToNearestIndex(actual, maxCount, _spriteStates.Count);
                if (_isOverlay)
                {
                    for (int i = 0; i < roundToLevels; i++)
                    {
                        spriteComponent.LayerSetVisible(_spriteStates[i], true);
                    }
                }
                else
                {
                    spriteComponent.LayerSetState(0, _spriteStates[roundToLevels]);
                }
            }
        }
    }
}
