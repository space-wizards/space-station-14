using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Light.EntitySystems;

public sealed class LightBulbSystem : SharedLightBulbSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightBulbComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LightBulbComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // update sprite state
        if (_appearance.TryGetData<LightBulbState>(uid, LightBulbVisuals.State, out var state, args.Component))
        {
            switch (state)
            {
                case LightBulbState.Normal:
                    _sprite.LayerSetRsiState((uid, args.Sprite), LightBulbVisualLayers.Base, comp.NormalSpriteState);
                    break;
                case LightBulbState.Broken:
                    _sprite.LayerSetRsiState((uid, args.Sprite), LightBulbVisualLayers.Base, comp.BrokenSpriteState);
                    break;
                case LightBulbState.Burned:
                    _sprite.LayerSetRsiState((uid, args.Sprite), LightBulbVisualLayers.Base, comp.BurnedSpriteState);
                    break;
            }
        }

        // also update sprites color
        if (_appearance.TryGetData<Color>(uid, LightBulbVisuals.Color, out var color, args.Component))
        {
            _sprite.SetColor((uid, args.Sprite), color);
        }
    }
}
