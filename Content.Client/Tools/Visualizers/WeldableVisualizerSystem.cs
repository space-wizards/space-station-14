using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WeldableVisualizerSystem : VisualizerSystem<WeldableComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, WeldableComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        args.Component.TryGetData(WeldableVisuals.IsWelded, out bool isWelded);
        if (sprite.LayerMapTryGet(WeldableLayers.BaseWelded, out var layer))
        {
            sprite.LayerSetVisible(layer, isWelded);
        }
    }
}
