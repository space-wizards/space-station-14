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
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<DoorComponent> _doorQuery;

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
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.OpeningLightSpriteState, 0f) },
                }
            );

            ((Animation)door.ClosingAnimation).AnimationTracks.Add(
                new AnimationTrackSpriteFlick
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

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
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

        if (!args.TryGetData<DoorState>(DoorVisuals.State, out var state))
            state = DoorState.Closed;

        var boltedVisible = args.TryGetData<bool>(DoorVisuals.BoltLights, out var lights) && lights;
        var unlitVisible =
            state == DoorState.Closing
            || state == DoorState.Opening
            || state == DoorState.Denying
            || args.TryGetData<bool>(DoorVisuals.ClosedLights, out var closedLights) && closedLights;

        if (_sprite.LayerMapTryGet((ent, args.Sprite), DoorVisualLayers.BaseUnlit, out var unlitLayer, logMissing: false))
            _sprite.LayerSetVisible((ent, args.Sprite), unlitLayer, unlitVisible && !boltedVisible);

        if (_sprite.LayerMapTryGet((ent, args.Sprite), DoorVisualLayers.BaseBolted, out var boltedLayer, logMissing: false))
            _sprite.LayerSetVisible((ent, args.Sprite), boltedLayer, boltedVisible);

        var warningLightsVisible =
            state == DoorState.Closed
            || state == DoorState.Welded
            || state == DoorState.Denying;

        if (_sprite.LayerMapTryGet((ent, args.Sprite), FirelockVisualLayersPressure.Base, out var pressureLayerIndex, logMissing: false))
        {
            if (!args.TryGetData<bool>(FirelockVisuals.PressureWarning, out var pressure))
                pressure = false;
            _sprite.LayerSetVisible((ent, args.Sprite), pressureLayerIndex, pressure && warningLightsVisible);
        }

        if (_sprite.LayerMapTryGet((ent, args.Sprite), FirelockVisualLayersTemperature.Base, out var tempLayerIndex, logMissing: false))
        {
            if (!args.TryGetData<bool>(FirelockVisuals.TemperatureWarning, out var temp))
                temp = false;
            _sprite.LayerSetVisible((ent, args.Sprite), tempLayerIndex, temp && warningLightsVisible);
        }
    }
}
