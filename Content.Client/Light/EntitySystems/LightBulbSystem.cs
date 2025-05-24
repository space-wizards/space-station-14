using Content.Shared.Light.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Light.Visualizers;

public sealed class LightBulbSystem : VisualizerSystem<LightBulbComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LightBulbComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // update sprite state
        if (AppearanceSystem.TryGetData<LightBulbState>(uid, LightBulbVisuals.State, out var state, args.Component))
        {
            switch (state)
            {
                case LightBulbState.Normal:
                    args.Sprite.LayerSetState(LightBulbVisualLayers.Base, comp.NormalSpriteState);
                    break;
                case LightBulbState.Broken:
                    args.Sprite.LayerSetState(LightBulbVisualLayers.Base, comp.BrokenSpriteState);
                    break;
                case LightBulbState.Burned:
                    args.Sprite.LayerSetState(LightBulbVisualLayers.Base, comp.BurnedSpriteState);
                    break;
            }
        }

        // also update sprites color
        if (AppearanceSystem.TryGetData<Color>(uid, LightBulbVisuals.Color, out var color, args.Component))
        {
            args.Sprite.Color = color;
        }
    }
}
