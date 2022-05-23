using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WeldableVisualizerSystem : VisualizerSystem<WeldableComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, WeldableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        args.Component.TryGetData(WeldableVisuals.IsWelded, out bool isWelded);
        if (args.Sprite.LayerMapTryGet(WeldableLayers.BaseWelded, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, isWelded);
        }
    }
}
