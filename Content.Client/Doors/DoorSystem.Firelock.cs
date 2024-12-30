using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed partial class DoorSystem
{
    private void InitializeClientFirelock()
    {
        SubscribeLocalEvent<FirelockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<FirelockComponent> firelock, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var boltedVisible = false;
        var unlitVisible = false;

        if (!_appearanceSystem.TryGetData<DoorState>(firelock, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        boltedVisible =
            _appearanceSystem.TryGetData<bool>(firelock, DoorVisuals.BoltLights, out var lights, args.Component) && lights;
        unlitVisible =
            state == DoorState.AttemptingCloseBySelf
            || state == DoorState.AttemptingOpenBySelf
            || state == DoorState.Denying
            || (_appearanceSystem.TryGetData<bool>(firelock,
                DoorVisuals.ClosedLights,
                out var closedLights,
                args.Component) && closedLights);

        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible && !boltedVisible);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
    }
}
