using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Systems;

public sealed partial class EmitterSystem : SharedEmitterSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(EntityUid uid, EmitterComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<EmitterVisualState>(EmitterVisuals.VisualState, out var state))
            state = EmitterVisualState.Off;

        if (!_sprite.LayerMapTryGet((uid, args.Sprite), EmitterVisualLayers.Lights, out var layer, false))
            return;

        switch (state)
        {
            case EmitterVisualState.On:
                if (component.OnState == null)
                    break;
                _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.OnState);
                break;
            case EmitterVisualState.Underpowered:
                if (component.UnderpoweredState == null)
                    break;
                _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.UnderpoweredState);
                break;
            case EmitterVisualState.Off:
                _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
