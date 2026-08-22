using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

/// <inheritdoc/>
public sealed partial class FirelockSystem : SharedFirelockSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    protected override void OnComponentStartup(Entity<FirelockComponent> ent, ref ComponentStartup args)
    {
        base.OnComponentStartup(ent, ref args);
        if (!TryComp<DoorComponent>(ent.Owner, out var door))
            return;

        door.ClosedSpriteStates.Add((DoorVisualLayers.BaseUnlit, ent.Comp.WarningLightSpriteState));
        door.OpenSpriteStates.Add((DoorVisualLayers.BaseUnlit, ent.Comp.WarningLightSpriteState));

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = DoorVisualLayers.BaseUnlit,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.OpeningLightSpriteState, 0f) },
        });

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = DoorVisualLayers.BaseUnlit,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.ClosingLightSpriteState, 0f) },
        });
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<FirelockComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<DoorState>(DoorVisuals.State, out var state))
            state = DoorState.Closed;

        var boltedVisible = args.TryGetData<bool>(DoorVisuals.BoltLights, out var lights) && lights;

        var unlitVisible = state == DoorState.Closing
            || state == DoorState.Opening
            || state == DoorState.Denying
            || args.TryGetData<bool>(DoorVisuals.ClosedLights, out var closedLights) && closedLights;

        _sprite.LayerSetVisible((ent, args.Sprite), DoorVisualLayers.BaseUnlit, unlitVisible && !boltedVisible);
        _sprite.LayerSetVisible((ent, args.Sprite), DoorVisualLayers.BaseBolted, boltedVisible);
    }
}
