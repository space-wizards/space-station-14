using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Power;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

/// <inheritdoc/>
public sealed partial class AirlockSystem : SharedAirlockSystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<DoorComponent> _doorQuery = default!;

    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<AirlockComponent> ent, ref ComponentStartup args)
    {
        // Has to be on component startup because we don't know what order components initialize in and running this before DoorComponent inits _will_ crash.
        if (!_doorQuery.TryComp(ent, out var door))
            return;

        if (ent.Comp.OpenUnlitVisible) // Otherwise there are flashes of the fallback sprite between clicking on the door and the door closing animation starting.
        {
            door.OpenSpriteStates.Add((DoorVisualLayers.BaseUnlit, ent.Comp.OpenSpriteState));
            door.ClosedSpriteStates.Add((DoorVisualLayers.BaseUnlit, ent.Comp.ClosedSpriteState));
        }

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = DoorVisualLayers.BaseUnlit,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.OpeningSpriteState, 0f) },
        }
        );

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = DoorVisualLayers.BaseUnlit,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.ClosingSpriteState, 0f) },
        }
        );

        door.DenyingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(ent.Comp.DenyAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.DenySpriteState, 0f) },
                }
            }
        };

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
    private void OnAppearanceChange(Entity<AirlockComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearanceSystem.TryGetData<DoorState>(ent, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        _appearanceSystem.TryGetData<bool>(ent, PowerDeviceVisuals.Powered, out var hasPower, args.Component);

        var showBaseUnlit = false;
        var showBolted = false;
        var showEmergency = false;

        if (hasPower)
        {
            _appearanceSystem.TryGetData<bool>(ent, DoorVisuals.BoltLights, out var boltedVisible, args.Component);
            showBolted = boltedVisible && (state == DoorState.Closed || state == DoorState.Welded);

            _appearanceSystem.TryGetData<bool>(ent, DoorVisuals.EmergencyLights, out var emergencyVisible, args.Component);
            showEmergency = emergencyVisible;

            if (!showBolted && !showEmergency)
            {
                if (state == DoorState.Closing || state == DoorState.Opening || state == DoorState.Denying || state == DoorState.Closed)
                    showBaseUnlit = true;

                if (state == DoorState.Open && ent.Comp.OpenUnlitVisible)
                    showBaseUnlit = true;

                _appearanceSystem.TryGetData<bool>(ent, DoorVisuals.ClosedLights, out var closedLightsVisible, args.Component);
                if (state == DoorState.Closed && closedLightsVisible)
                    showBaseUnlit = true;
            }
        }

        _sprite.LayerSetVisible((ent, args.Sprite), DoorVisualLayers.BaseUnlit, showBaseUnlit);
        _sprite.LayerSetVisible((ent, args.Sprite), DoorVisualLayers.BaseBolted, showBolted);
        if (ent.Comp.EmergencyAccessLayer)
        {
            var isDoorIdle = state != DoorState.Open && state != DoorState.Opening && state != DoorState.Closing;
            _sprite.LayerSetVisible((ent, args.Sprite), DoorVisualLayers.BaseEmergencyAccess,
                showEmergency && isDoorIdle && !showBolted);
        }

        if (ent.Comp.OpenUnlitVisible)
        {
            switch (state)
            {
                case DoorState.Open:
                    _sprite.LayerSetRsiState((ent, args.Sprite), DoorVisualLayers.BaseUnlit, ent.Comp.OpenSpriteState);
                    break;
                case DoorState.Closed:
                    _sprite.LayerSetRsiState((ent, args.Sprite), DoorVisualLayers.BaseUnlit, ent.Comp.ClosedSpriteState);
                    break;
            }
        }
    }
}
