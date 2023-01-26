using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WeldableVisualizerSystem : VisualizerSystem<WeldableComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, WeldableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _appearance.TryGetData(uid, WeldableVisuals.IsWelded, out bool isWelded, args.Component);
        if (args.Sprite.LayerMapTryGet(WeldableLayers.BaseWelded, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, isWelded);
        }
    }
}
