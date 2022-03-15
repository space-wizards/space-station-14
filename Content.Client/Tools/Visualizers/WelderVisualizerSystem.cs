using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WelderVisualizerSystem : VisualizerSystem<WelderComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, WelderComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (args.Component.TryGetData(WelderVisuals.Lit, out bool isLit))
        {
            sprite.LayerSetVisible(WelderLayers.Flame, isLit);
        }
    }
}
