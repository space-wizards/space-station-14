using Content.Shared.Nutrition.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.Visualizers;

[RegisterComponent]
public sealed class DrinkCanVisualizerSystem : VisualizerSystem<DrinkCanVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DrinkCanVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, DrinkCanStateVisual.Opened, out var opened, args.Component) && opened)
        {
            args.Sprite.LayerSetState(DrinkCanVisualLayers.Icon, $"{comp.StateOpen}");
            return;
        }

        args.Sprite.LayerSetState(DrinkCanVisualLayers.Icon, $"{comp.StateClosed}");
    }
}

public enum DrinkCanVisualLayers : byte
{
    Icon = 0
}
