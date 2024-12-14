using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed class FirelockSystem : SharedFirelockSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FirelockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, FirelockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var boltedVisible = false;
        var unlitVisible = false;

        if (!_appearanceSystem.TryGetData<DoorState>(uid, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        boltedVisible = _appearanceSystem.TryGetData<bool>(uid, DoorVisuals.BoltLights, out var lights, args.Component) && lights;
        unlitVisible =
            state == DoorState.Closing
            ||  state == DoorState.Opening
            ||  state == DoorState.Denying
            || (_appearanceSystem.TryGetData<bool>(uid, DoorVisuals.ClosedLights, out var closedLights, args.Component) && closedLights);

        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible && !boltedVisible);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
    }
}
