using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Power.EntitySystems;

/// <inheritdoc/>
public sealed partial class PowerStateSystem : SharedPowerStateSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    /// <summary> Updates visual appearance according to provided changes. </summary>
    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<PowerStateComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var uid = ent.Owner;
        var component = ent.Comp;

        if (!_appearance.TryGetData<PowerStateDeviceVisualState>(uid, PowerStateDeviceVisuals.VisualState, out var state, args.Component))
            state = PowerStateDeviceVisualState.Off;

        if (!_sprite.LayerMapTryGet((uid, args.Sprite), PowerStateDeviceVisualLayers.Lights, out var layer, false))
            return;

        switch (state)
        {
            case PowerStateDeviceVisualState.On:
                if (component.WorkingState == null)
                    break;
                _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.WorkingState);
                break;
            case PowerStateDeviceVisualState.Underpowered:
                if (component.UnderpoweredState == null)
                    break;
                _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.UnderpoweredState);
                break;
            case PowerStateDeviceVisualState.Off:
                _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
