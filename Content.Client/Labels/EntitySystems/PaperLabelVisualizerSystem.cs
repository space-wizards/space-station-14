using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Labels.EntitySystems;

/// <summary>
/// A system that updates the sprites and color of an entity.
/// </summary>
/// <seealso cref="PaperLabelComponent"/>
public sealed partial class PaperLabelVisualizerSystem : VisualizerSystem<PaperLabelComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, PaperLabelComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), PaperLabelVisuals.Layer, out var layerId, logMissing: false))
            return;

        if (!AppearanceSystem.TryGetData<PaperLabelType>(uid, PaperLabelVisuals.LabelType, out var labelType)
            || labelType == PaperLabelType.None)
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layerId, false);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layerId, true);
            var state = component.LabelStates.GetValueOrDefault(labelType) ?? component.FallbackLabelState;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerId, state);

            if (component.Recolor)
            {
                if (!AppearanceSystem.TryGetData<Color>(uid, PaperLabelVisuals.LabelColor, out var color))
                    color = Color.White;
                SpriteSystem.LayerSetColor((uid, args.Sprite), layerId, color);
            }
        }
    }
}
