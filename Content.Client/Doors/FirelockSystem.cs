using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;

namespace Content.Client.Doors;

public sealed class FirelockSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FirelockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, FirelockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_gameTiming.IsFirstTimePredicted)
            return;

        // Apply the closed lights bool to the sprite
        bool unlitVisible =
            (AppearanceSystem.TryGetData<bool>(uid, DoorVisuals.ClosedLights, out var closedLights, args.Component) &&
             closedLights);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
    }
}
