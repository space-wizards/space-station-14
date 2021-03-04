#nullable enable
using System;
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [UsedImplicitly]
    public class SolutionContainerVisualizer : AppearanceVisualizer
    {
        private int _maxFillLevels;
        private string? _fillBaseName;
        private string? _emptySpriteName;
        private SolutionContainerLayers _layer;
        private bool _changeColor;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var serializer = YamlObjectSerializer.NewReader(node);
            serializer.DataField(ref _maxFillLevels, "maxFillLevels", 0);
            serializer.DataField(ref _fillBaseName, "fillBaseName", null);

            serializer.DataField(ref _emptySpriteName, "emptySpriteName", null);
            serializer.DataField(ref _layer, "layer", SolutionContainerLayers.Fill);
            serializer.DataField(ref _changeColor, "changeColor", true);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (_maxFillLevels <= 0 || _fillBaseName == null) return;

            if (!component.TryGetData(SolutionContainerVisuals.VisualState,
                out SolutionContainerVisualState state)) return;

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;
            if (!sprite.LayerMapTryGet(_layer, out var fillLayer)) return;

            var fillPercent = state.FilledVolumePercent;
            var closestFillSprite = (int) Math.Round(fillPercent * _maxFillLevels);

            if (closestFillSprite > 0)
            {
                sprite.LayerSetVisible(fillLayer, true);

                var stateName = _fillBaseName + closestFillSprite;
                sprite.LayerSetState(fillLayer, stateName);

                if (_changeColor)
                    sprite.LayerSetColor(fillLayer, state.Color);
            }
            else
            {
                if (_emptySpriteName == null)
                    sprite.LayerSetVisible(fillLayer, false);
                else
                    sprite.LayerSetState(fillLayer, _emptySpriteName);
            }
        }
    }
}
