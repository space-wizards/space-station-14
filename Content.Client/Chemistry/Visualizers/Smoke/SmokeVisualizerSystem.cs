using Content.Shared.Smoking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers.Smoke;

[UsedImplicitly]
public sealed class SmokeVisualizerSystem : VisualizerSystem<SmokeVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SmokeVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.Component.TryGetData<Color>(SmokeVisuals.Color, out var color) || args.Sprite == null)
        {
            return;
        }

        args.Sprite.Color = color;
    }
}
