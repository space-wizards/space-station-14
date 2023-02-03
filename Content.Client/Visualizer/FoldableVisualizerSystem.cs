using Robust.Client.GameObjects;
using static Content.Shared.Foldable.SharedFoldableSystem;

namespace Content.Client.Visualizer;

public sealed class FoldableVisualizerSystem : VisualizerSystem<FoldableVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FoldableVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, FoldedVisuals.State, out bool folded, args.Component) && folded)
        {
            args.Sprite.LayerSetState(FoldableVisualLayers.Base, $"{comp.Key}_folded");
        }
        else
        {
            args.Sprite.LayerSetState(FoldableVisualLayers.Base, $"{comp.Key}");
        }
    }
}

public enum FoldableVisualLayers : byte
{
    Base,
}
