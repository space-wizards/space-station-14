using Content.Shared.MobState;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.MobState;

public sealed class DamageStateVisualizerSystem : VisualizerSystem<DamageStateVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DamageStateVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !args.Component.TryGetData(DamageStateVisuals.State, out DamageState data))
        {
            return;
        }

        if (!component.States.TryGetValue(data, out var layers))
        {
            return;
        }

        if (component.Rotate)
        {
            sprite.NoRotation = data switch
            {
                DamageState.Critical => false,
                DamageState.Dead => false,
                _ => true
            };
        }

        // Brain no worky rn so this was just easier.
        foreach (var key in new []{ DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!sprite.LayerMapTryGet(key, out _)) continue;

            sprite.LayerSetVisible(key, false);
        }

        foreach (var (key, state) in layers)
        {
            // Inheritance moment.
            if (!sprite.LayerMapTryGet(key, out _)) continue;

            sprite.LayerSetVisible(key, true);
            sprite.LayerSetState(key, state);
        }

        // So they don't draw over mobs anymore
        if (data == DamageState.Dead)
        {
            if (sprite.DrawDepth > (int) DrawDepth.FloorObjects)
            {
                component.OriginalDrawDepth = sprite.DrawDepth;
                sprite.DrawDepth = (int) DrawDepth.FloorObjects;
            }
        }
        else if (component.OriginalDrawDepth != null)
        {
            sprite.DrawDepth = component.OriginalDrawDepth.Value;
            component.OriginalDrawDepth = null;
        }
    }
}
