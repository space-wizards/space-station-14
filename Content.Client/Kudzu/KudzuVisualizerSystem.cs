using Content.Shared.Kudzu;
using Robust.Client.GameObjects;

namespace Content.Client.Kudzu
{

    public sealed class KudzuVisualsSystem : VisualizerSystem<KudzuVisualizerComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, KudzuVisualizerComponent component, ref AppearanceChangeEvent args)
        {

            if (!TryComp(uid, out SpriteComponent? sprite))
            {
                return;
            }
            if (args.Component.TryGetData(KudzuVisuals.Variant, out int var)
                && args.Component.TryGetData(KudzuVisuals.GrowthLevel, out int level))
            {
                sprite.LayerMapReserveBlank(component.Layer);
                sprite.LayerSetState(0, $"kudzu_{level}{var}");
            }
        }
    }
}
