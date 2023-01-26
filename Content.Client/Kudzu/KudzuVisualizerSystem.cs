using Content.Shared.Kudzu;
using Robust.Client.GameObjects;

namespace Content.Client.Kudzu;

public sealed class KudzuVisualsSystem : VisualizerSystem<KudzuVisualsComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, KudzuVisualsComponent component, ref AppearanceChangeEvent args)
    {

        if (args.Sprite == null)
            return;
        if (_appearance.TryGetData(uid, KudzuVisuals.Variant, out int var, args.Component)
            && _appearance.TryGetData(uid, KudzuVisuals.GrowthLevel, out int level, args.Component))
        {
            var index = args.Sprite.LayerMapReserveBlank(component.Layer);
            args.Sprite.LayerSetState(index, $"kudzu_{level}{var}");
        }
    }
}
