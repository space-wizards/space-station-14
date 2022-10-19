using Content.Client.Radiation.Components;
using Content.Shared.Radiation.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerVisualizerSystem : VisualizerSystem<GeigerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GeigerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        // check the sprite
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;
        if (!sprite.LayerMapTryGet(GeigerLayers.Screen, out var layer))
            return;

        // enable screen
        if (!args.Component.TryGetData(GeigerVisuals.IsEnabled, out bool isEnabled))
            return;
        sprite.LayerSetVisible(layer, isEnabled);

        // set right alert level
        if (!args.Component.TryGetData(GeigerVisuals.DangerLevel, out GeigerDangerLevel level))
            return;
        if (!component.States.TryGetValue(level, out var state))
            return;
        sprite.LayerSetState(layer, state);
    }
}
