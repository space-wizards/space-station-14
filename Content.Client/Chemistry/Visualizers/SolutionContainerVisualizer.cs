using System;
using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Chemistry.Visualizers
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

            if (!component.TryGetData(SolutionContainerVisuals.VisualState,
                out SolutionContainerVisualState state)) return;

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite)) return;
            if (!sprite.LayerMapTryGet(_layer, out var fillLayer)) return;

            var fillPercent = state.FilledVolumePercent;
            var closestFillSprite = (int) Math.Round(fillPercent * _maxFillLevels);

            if (closestFillSprite > 0)
            {
                if (_fillBaseName == null) return;

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
