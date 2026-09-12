using Content.Shared.Paper;
using Robust.Client.GameObjects;

using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper;

public sealed partial class PaperVisualizerSystem : VisualizerSystem<PaperVisualizerComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, PaperVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.TryGetData<PaperStatus>(PaperVisuals.Status, out var writingStatus))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);

        if (args.TryGetData<string>(PaperVisuals.Stamp, out var stampState))
        {
            if (stampState != string.Empty)
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PaperVisualLayers.Stamp, stampState);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, true);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, false);
            }

        }
    }
}

/// <summary>
/// Sprite mapping enum.
/// </summary>
public enum PaperVisualLayers
{
    Stamp,
    Writing,
}
