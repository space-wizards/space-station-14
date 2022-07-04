using Content.Shared.Kudzu;
using Robust.Client.GameObjects;

namespace Content.Client.Kudzu
{

    public sealed class KudzuVisualsSystem : VisualizerSystem<KudzuVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, KudzuVisualsComponent component, ref AppearanceChangeEvent args)
        {

            if (args.Sprite == null)
                return;
            if (args.Component.TryGetData(KudzuVisuals.Variant, out int var)
                && args.Component.TryGetData(KudzuVisuals.GrowthLevel, out int level))
            {
                var index = args.Sprite.LayerMapReserveBlank(component.Layer);
                args.Sprite.LayerSetState(index, $"kudzu_{level}{var}");
            }
        }
    }
}
