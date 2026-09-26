using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

// TODO: Consolidate redundant code from the AirlockSystem.

/// <inheritdoc/>
public sealed partial class FirelockSystem : SharedFirelockSystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<DoorComponent> _doorQuery = default!;

    /// <inheritdoc/>
    protected override void OnComponentStartup(Entity<FirelockComponent> ent, ref ComponentStartup args)
    {
        base.OnComponentStartup(ent, ref args);
        if (!_doorQuery.TryComp(ent.Owner, out var door))
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

        if (!ent.Comp.AnimatePanel)
            return;

        door.OpenSpriteStates.Add((WiresVisualLayers.MaintenancePanel, null));
        door.ClosedSpriteStates.Add((WiresVisualLayers.MaintenancePanel, ent.Comp.OpenPanelSpriteState));

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.OpeningPanelSpriteState, 0f) },
        });

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.ClosingPanelSpriteState, 0f) },
        });
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<FirelockComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearanceSystem.TryGetData<DoorState>(ent, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        var boltedVisible = _appearanceSystem.TryGetData<bool>(ent, DoorVisuals.BoltLights, out var lights, args.Component) && lights;
        var unlitVisible =
            state == DoorState.Closing
            || state == DoorState.Opening
            || state == DoorState.Denying
            || _appearanceSystem.TryGetData<bool>(ent, DoorVisuals.ClosedLights, out var closedLights, args.Component) && closedLights;

        if (_sprite.LayerMapTryGet((ent, args.Sprite), DoorVisualLayers.BaseUnlit, out var unlitLayer, logMissing: false))
            _sprite.LayerSetVisible((ent, args.Sprite), unlitLayer, unlitVisible && !boltedVisible);

        if (_sprite.LayerMapTryGet((ent, args.Sprite), DoorVisualLayers.BaseBolted, out var boltedLayer, logMissing: false))
            _sprite.LayerSetVisible((ent, args.Sprite), boltedLayer, boltedVisible);
    }
}
