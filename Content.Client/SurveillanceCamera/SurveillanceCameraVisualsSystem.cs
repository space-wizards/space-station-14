using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera;

public sealed partial class SurveillanceCameraVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, SurveillanceCameraVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.TryGetData<SurveillanceCameraVisuals>(SurveillanceCameraVisualsKey.Key, out var key)
            || !_sprite.LayerMapTryGet((uid, args.Sprite), SurveillanceCameraVisualsKey.Layer, out var layer, false)
            || !component.CameraSprites.TryGetValue(key, out var state))
        {
            return;
        }

        _sprite.LayerSetRsiState((uid, args.Sprite), layer, state);
    }
}
