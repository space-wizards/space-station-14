#nullable enable
using System;
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [UsedImplicitly]
    public class SolutionContainerVisualizer : AppearanceVisualizer
    {
        [DataField("maxFillLevels")] private int _maxFillLevels = 0;
        [DataField("fillBaseName")] private string? _fillBaseName = null;
        [DataField("layer")] private SolutionContainerLayers _layer = SolutionContainerLayers.Fill;
        [DataField("changeColor")] private bool _changeColor = true;
        [DataField("emptySpriteName")] private string? _emptySpriteName = null;
        [DataField("emptySpriteColor")] private Color _emptySpriteColor = Color.White;

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
                {
                    sprite.LayerSetState(fillLayer, _emptySpriteName);
                    if (_changeColor)
                        sprite.LayerSetColor(fillLayer, _emptySpriteColor);
                }
            }
        }
    }
}
