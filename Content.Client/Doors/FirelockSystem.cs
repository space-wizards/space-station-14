using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed class FirelockSystem : EntitySystem
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

        // Apply the closed lights bool to the sprite
        var unlitVisible = _appearanceSystem.TryGetData<bool>(uid, DoorVisuals.ClosedLights, out var closedLights, args.Component) && closedLights;
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
    }
}
