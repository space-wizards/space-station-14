using Robust.Client.GameObjects;

using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

public sealed class PaperVisualizerSystem : VisualizerSystem<PaperVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PaperVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<PaperStatus>(uid, PaperVisuals.Status, out var writingStatus, args.Component))
            args.Sprite.LayerSetVisible(PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);

        if (AppearanceSystem.TryGetData<string>(uid, PaperVisuals.Stamp, out var stampState, args.Component))
        {
            if (stampState != string.Empty)
            {
                args.Sprite.LayerSetState(PaperVisualLayers.Stamp, stampState);
                args.Sprite.LayerSetVisible(PaperVisualLayers.Stamp, true);
            }
            else
            {
                args.Sprite.LayerSetVisible(PaperVisualLayers.Stamp, false);
            }

        }
    }
}

public enum PaperVisualLayers
{
    Stamp,
    Writing
}
