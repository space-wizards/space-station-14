using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed partial class DoorSystem
{
    private void InitializeClientFirelock()
    {
        SubscribeLocalEvent<DoorAlarmComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<DoorAlarmComponent> doorAlarm, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearanceSystem.TryGetData<DoorState>(doorAlarm, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        var boltedVisible = _appearanceSystem.TryGetData<bool>(doorAlarm, DoorVisuals.BoltLights, out var lights, args.Component) && lights;
        var unlitVisible = state == DoorState.AttemptingCloseBySelf
                           || state == DoorState.AttemptingOpenBySelf
                           || state == DoorState.Denying
                           || _appearanceSystem.TryGetData<bool>(doorAlarm,
                               DoorVisuals.ClosedLights,
                               out var closedLights,
                               args.Component) && closedLights;

        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible && !boltedVisible);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
    }
}
