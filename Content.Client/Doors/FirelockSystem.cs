using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed partial class FirelockSystem : SharedFirelockSystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FirelockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnComponentStartup(Entity<FirelockComponent> ent, ref ComponentStartup args)
    {
        base.OnComponentStartup(ent, ref args);
        if (!TryComp<DoorComponent>(ent.Owner, out var door))
            return;

        // Add animations if we have an unlit layer.
        if (_sprite.LayerMapTryGet(ent.Owner, DoorVisualLayers.BaseUnlit, out _, logMissing: false))
        {
            door.ClosedSpriteStates.Add((DoorVisualLayers.BaseUnlit, ent.Comp.WarningLightSpriteState));
            door.OpenSpriteStates.Add((DoorVisualLayers.BaseUnlit, ent.Comp.WarningLightSpriteState));

            ((Animation)door.OpeningAnimation).AnimationTracks.Add(
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.OpeningLightSpriteState, 0f) },
                }
            );

            ((Animation)door.ClosingAnimation).AnimationTracks.Add(
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.ClosingLightSpriteState, 0f) },
                }
            );
        }
    }

    private void OnAppearanceChange(EntityUid uid, FirelockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearanceSystem.TryGetData<DoorState>(uid, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        var boltedVisible = _appearanceSystem.TryGetData<bool>(uid, DoorVisuals.BoltLights, out var lights, args.Component) && lights;
        var unlitVisible =
            state == DoorState.Closing
            || state == DoorState.Opening
            || state == DoorState.Denying
            || _appearanceSystem.TryGetData<bool>(uid, DoorVisuals.ClosedLights, out var closedLights, args.Component) && closedLights;

        if (_sprite.LayerMapTryGet((uid, args.Sprite), DoorVisualLayers.BaseUnlit, out var unlitLayer, logMissing: false))
            _sprite.LayerSetVisible((uid, args.Sprite), unlitLayer, unlitVisible && !boltedVisible);

        if (_sprite.LayerMapTryGet((uid, args.Sprite), DoorVisualLayers.BaseBolted, out var boltedLayer, logMissing: false))
            _sprite.LayerSetVisible((uid, args.Sprite), boltedLayer, boltedVisible);
    }
}
