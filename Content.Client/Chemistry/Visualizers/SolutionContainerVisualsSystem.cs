using Content.Shared.Chemistry;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

public sealed class SolutionContainerVisualsSystem : VisualizerSystem<SolutionContainerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SolutionContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!args.Component.TryGetData(SolutionContainerVisuals.VisualState, out SolutionContainerVisualState state))
            return;

        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(component.Layer, out var fillLayer))
            return;

        var fillPercent = state.FilledVolumePercent;
        var closestFillSprite = (int) Math.Round(fillPercent * component.MaxFillLevels);

        if (closestFillSprite > 0)
        {
            if (component.FillBaseName == null)
                return;

            args.Sprite.LayerSetVisible(fillLayer, true);

            var stateName = component.FillBaseName + closestFillSprite;
            args.Sprite.LayerSetState(fillLayer, stateName);

            if (component.ChangeColor)
                args.Sprite.LayerSetColor(fillLayer, state.Color);
        }
        else
        {
            if (component.EmptySpriteName == null)
                args.Sprite.LayerSetVisible(fillLayer, false);
            else
            {
                args.Sprite.LayerSetState(fillLayer, component.EmptySpriteName);
                if (component.ChangeColor)
                    args.Sprite.LayerSetColor(fillLayer, component.EmptySpriteColor);
            }
        }
    }
}
