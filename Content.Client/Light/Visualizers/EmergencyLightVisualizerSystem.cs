using Content.Shared.Light.Component;
using Robust.Client.GameObjects;

namespace Content.Client.Light.Visualizers;

public sealed class EmergencyLightVisualizerSystem : VisualizerSystem<EmergencyLightVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EmergencyLightVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, EmergencyLightVisuals.On, out bool on, args.Component))
            on = false;

        args.Sprite.LayerSetState(EmergencyLightVisualLayers.Light, on ? "emergency_light_on" : "emergency_light_off");
        args.Sprite.LayerSetShader(EmergencyLightVisualLayers.Light, on ? "unshaded" : "shaded");

        if (AppearanceSystem.TryGetData(uid, EmergencyLightVisuals.Color, out Color color, args.Component))
        {
            args.Sprite.LayerSetColor(EmergencyLightVisualLayers.Light, color);
        }
    }
}

public enum EmergencyLightVisualLayers
{
    Base,
    Light
}
