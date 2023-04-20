using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WelderVisualizerSystem : VisualizerSystem<WelderComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, WelderComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, WelderVisuals.Lit, out var isLit, args.Component))
        {
            args.Sprite.LayerSetVisible(WelderLayers.Flame, isLit);
        }
    }
}
