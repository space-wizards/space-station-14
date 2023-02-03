using Content.Shared.Smoking;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

public sealed class SmokeVisualizerSystem : VisualizerSystem<SmokeVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SmokeVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<Color>(uid, SmokeVisuals.Color, out var color))
            return;
        args.Sprite.Color = color;
    }
}
