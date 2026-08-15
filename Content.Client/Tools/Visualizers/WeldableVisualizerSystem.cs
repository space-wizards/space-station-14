using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed partial class WeldableVisualizerSystem : VisualizerSystem<WeldableComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, WeldableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        args.TryGetData<bool>(WeldableVisuals.IsWelded, out var isWelded);
        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), WeldableLayers.BaseWelded, out var layer, false))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, isWelded);
        }
    }
}
