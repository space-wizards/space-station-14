using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Systems;

public sealed class EmitterSystem : SharedEmitterSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EmitterComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, EmitterComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<EmitterVisualState>(uid, EmitterVisuals.VisualState, out var state, args.Component))
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
