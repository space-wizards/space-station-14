using Content.Shared.Mobs;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.DamageState;

public sealed class DamageStateVisualizerSystem : VisualizerSystem<DamageStateVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DamageStateVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData<MobState>(uid, MobStateVisuals.State, out var data, args.Component))
        {
            return;
        }

        if (!component.States.TryGetValue(data, out var layers))
        {
            return;
        }

        // Brain no worky rn so this was just easier.
        foreach (var key in new[] { DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), key, out _, false)) continue;

            SpriteSystem.LayerSetVisible((uid, sprite), key, false);
        }

        foreach (var (key, state) in layers)
        {
            // Inheritance moment.
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), key, out _, false)) continue;

            SpriteSystem.LayerSetVisible((uid, sprite), key, true);
            SpriteSystem.LayerSetRsiState((uid, sprite), key, state);
        }

        // So they don't draw over mobs anymore
        if (data == MobState.Dead)
        {
            if (sprite.DrawDepth > (int)DrawDepth.DeadMobs)
            {
                component.OriginalDrawDepth = sprite.DrawDepth;
                SpriteSystem.SetDrawDepth((uid, sprite), (int)DrawDepth.DeadMobs);
            }
        }
        else if (component.OriginalDrawDepth != null)
        {
            SpriteSystem.SetDrawDepth((uid, sprite), component.OriginalDrawDepth.Value);
            component.OriginalDrawDepth = null;
        }
    }
}
