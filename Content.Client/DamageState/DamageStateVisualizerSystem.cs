using Content.Shared.Mobs;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.DamageState;

public sealed partial class DamageStateVisualizerSystem : VisualizerSystem<DamageStateVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, DamageStateVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !args.TryGetData<MobState>(MobStateVisuals.State, out var data))
            return;

        if (!component.States.TryGetValue(data, out var layers))
            return;

        // Brain no worky rn so this was just easier.
        foreach (var key in new[] { DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), key, out var layerIndex, false)) continue;

            SpriteSystem.LayerSetVisible((uid, sprite), layerIndex, false);
        }

        foreach (var (key, state) in layers)
        {
            // Inheritance moment.
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), key, out var layerIndex, false)) continue;

            SpriteSystem.LayerSetVisible((uid, sprite), layerIndex, true);
            SpriteSystem.LayerSetRsiState((uid, sprite), layerIndex, state);
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
