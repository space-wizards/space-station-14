using Content.Shared.Spider;
using Robust.Client.GameObjects;

namespace Content.Client.Spider
{

    public sealed class SpiderVisualsSystem : VisualizerSystem<SpiderWebVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, SpiderWebVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;
            if (args.Component.TryGetData(SpiderWebVisuals.Variant, out int var))
                args.Sprite.LayerSetState(0, $"spider_web_{var}");
        }
    }
}
