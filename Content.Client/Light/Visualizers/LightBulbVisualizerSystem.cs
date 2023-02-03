using Content.Shared.Light;
using Robust.Client.GameObjects;

namespace Content.Client.Light.Visualizers;

public sealed class LightBulbVisualizerSystem : VisualizerSystem<LightBulbVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LightBulbVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // update sprite state
        if (AppearanceSystem.TryGetData(uid, LightBulbVisuals.State, out LightBulbState state, args.Component))
        {
            switch (state)
            {
                case LightBulbState.Normal:
                    args.Sprite.LayerSetState(0, "normal");
                    break;
                case LightBulbState.Broken:
                    args.Sprite.LayerSetState(0, "broken");
                    break;
                case LightBulbState.Burned:
                    args.Sprite.LayerSetState(0, "burned");
                    break;
            }
        }

        // also update sprites color
        if (AppearanceSystem.TryGetData(uid, LightBulbVisuals.Color, out Color color, args.Component))
        {
            args.Sprite.Color = color;
        }
    }
}
