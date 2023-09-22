using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WeldableVisualizerSystem : VisualizerSystem<WeldableComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, WeldableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<bool>(uid, WeldableVisuals.IsWelded, out var isWelded, args.Component);
        if (args.Sprite.LayerMapTryGet(WeldableLayers.BaseWelded, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, isWelded);
        }
    }
}
