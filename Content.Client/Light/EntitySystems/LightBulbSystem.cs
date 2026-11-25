using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Light.EntitySystems;

public sealed partial class LightBulbSystem : SharedLightBulbSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<LightBulbComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // update sprite state
        if (args.TryGetData<LightBulbState>(LightBulbVisuals.State, out var state))
        {
            switch (state)
            {
                case LightBulbState.Normal:
                    _sprite.LayerSetRsiState((ent, args.Sprite), LightBulbVisualLayers.Base, ent.Comp.NormalSpriteState);
                    break;
                case LightBulbState.Broken:
                    _sprite.LayerSetRsiState((ent, args.Sprite), LightBulbVisualLayers.Base, ent.Comp.BrokenSpriteState);
                    break;
                case LightBulbState.Burned:
                    _sprite.LayerSetRsiState((ent, args.Sprite), LightBulbVisualLayers.Base, ent.Comp.BurnedSpriteState);
                    break;
            }
        }

        // also update sprites color
        if (args.TryGetData<Color>(LightBulbVisuals.Color, out var color))
        {
            _sprite.SetColor((ent, args.Sprite), color);
        }
    }
}
