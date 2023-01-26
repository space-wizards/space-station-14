using Robust.Client.GameObjects;

using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Client.Paper;

public sealed class PaperSystem : VisualizerSystem<PaperVisualsComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, PaperVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData(uid, PaperVisuals.Status , out PaperStatus writingStatus, args.Component))
            args.Sprite.LayerSetVisible(PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);

        if (_appearance.TryGetData(uid, PaperVisuals.Stamp, out string stampState, args.Component))
        {
            args.Sprite.LayerSetState(PaperVisualLayers.Stamp, stampState);
            args.Sprite.LayerSetVisible(PaperVisualLayers.Stamp, true);
        }
    }
}

public enum PaperVisualLayers
{
    Stamp,
    Writing
}
