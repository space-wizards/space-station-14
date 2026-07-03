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
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), WeldableLayers.BaseWelded, out var layer, false))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, isWelded);
        }
    }
}
