using Content.Shared.Spreader;
using Robust.Client.GameObjects;

namespace Content.Client.Kudzu;

public sealed class KudzuVisualsSystem : VisualizerSystem<KudzuVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, KudzuVisualsComponent component, ref AppearanceChangeEvent args)
    {

        if (args.Sprite == null)
            return;
        if (AppearanceSystem.TryGetData<int>(uid, KudzuVisuals.Variant, out var var, args.Component)
            && AppearanceSystem.TryGetData<int>(uid, KudzuVisuals.GrowthLevel, out var level, args.Component))
        {
            var index = SpriteSystem.LayerMapReserve((uid, args.Sprite), $"{component.Layer}");
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), index, $"kudzu_{level}{var}");
        }
    }
}
