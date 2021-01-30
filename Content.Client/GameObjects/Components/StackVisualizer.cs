#nullable enable

using System.Collections.Generic;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class StackVisualizer : AppearanceVisualizer
    {
        private List<string> _spritestates = new();
        private int _spriteCount;

        public override void LoadData(YamlMappingNode mapping)
        {
            base.LoadData(mapping);

            if (!mapping.TryGetNode("spritestates", out YamlSequenceNode? stepsMapping))
            {
                return;
            }

            foreach (var yamlNode in stepsMapping)
            {
                var spriteState = ((YamlScalarNode) yamlNode).Value;
                _spritestates.Add(spriteState!);
            }
            _spriteCount = _spritestates.Count;
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                if (component.TryGetData(StackVisuals.Count, out float data))
                {
                    spriteComponent.LayerSetState(0, _spritestates[extractIndex(data)] );
                }
            }

        }

        private int extractIndex(float percentile)
        {
            var index = (int) (percentile * _spriteCount);
            if (index >= _spriteCount)
            {
                index = 0;
            }

            return index;
        }
    }
}
