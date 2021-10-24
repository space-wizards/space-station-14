using Content.Shared.Light;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public class LightBulbVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                return;

            // update sprite state
            if (component.TryGetData(LightBulbVisuals.State, out LightBulbState state))
            {
                switch (state)
                {
                    case LightBulbState.Normal:
                        sprite.LayerSetState(0, "normal");
                        break;
                    case LightBulbState.Broken:
                        sprite.LayerSetState(0, "broken");
                        break;
                    case LightBulbState.Burned:
                        sprite.LayerSetState(0, "burned");
                        break;
                }
            }

            // also update sprites color
            if (component.TryGetData(LightBulbVisuals.Color, out Color color))
            {
                sprite.Color = color;
            }
        }
    }
}
