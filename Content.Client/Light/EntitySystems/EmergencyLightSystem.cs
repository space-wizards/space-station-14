using Content.Client.Light.Components;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Light.EntitySystems;

public sealed partial class EmergencyLightSystem : VisualizerSystem<EmergencyLightComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EmergencyLightComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<bool>(EmergencyLightVisuals.On, out var on))
            on = false;

        SpriteSystem.LayerSetVisible((uid, args.Sprite), EmergencyLightVisualLayers.LightOff, !on);
        SpriteSystem.LayerSetVisible((uid, args.Sprite), EmergencyLightVisualLayers.LightOn, on);

        if (args.TryGetData<Color>(EmergencyLightVisuals.Color, out var color))
        {
            SpriteSystem.LayerSetColor((uid, args.Sprite), EmergencyLightVisualLayers.LightOn, color);
            SpriteSystem.LayerSetColor((uid, args.Sprite), EmergencyLightVisualLayers.LightOff, color);
        }
    }
}
