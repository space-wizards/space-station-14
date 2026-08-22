using Content.Shared.Spreader;
using Robust.Client.GameObjects;

namespace Content.Client.Kudzu;

public sealed partial class KudzuVisualsSystem : VisualizerSystem<KudzuVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, KudzuVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.TryGetData<int>(KudzuVisuals.Variant, out var variant)
            || !args.TryGetData<int>(KudzuVisuals.GrowthLevel, out var level))
            return;

        var index = SpriteSystem.LayerMapReserve((uid, args.Sprite), $"{component.Layer}");
        SpriteSystem.LayerSetRsiState((uid, args.Sprite), index, $"kudzu_{level}{variant}");
    }
}
