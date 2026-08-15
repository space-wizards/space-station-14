using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Power;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed partial class AirlockSystem : SharedAirlockSystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AirlockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentStartup(EntityUid uid, AirlockComponent comp, ComponentStartup args)
    {
        // Has to be on component startup because we don't know what order components initialize in and running this before DoorComponent inits _will_ crash.
        if (!TryComp<DoorComponent>(uid, out var door))
            return;

        if (comp.OpenUnlitVisible) // Otherwise there are flashes of the fallback sprite between clicking on the door and the door closing animation starting.
        {
            door.OpenSpriteStates.Add((DoorVisualLayers.BaseUnlit, comp.OpenSpriteState));
            door.ClosedSpriteStates.Add((DoorVisualLayers.BaseUnlit, comp.ClosedSpriteState));
        }

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = DoorVisualLayers.BaseUnlit,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningSpriteState, 0f) },
        }
        );

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = DoorVisualLayers.BaseUnlit,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingSpriteState, 0f) },
        }
        );

        door.DenyingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.DenyAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.DenySpriteState, 0f) },
                }
            }
        };

        if (!comp.AnimatePanel)
            return;

        // For some reason the open panel sprite is used for both open and
        // closed sprites. I really don't get it.
        door.OpenSpriteStates.Add((WiresVisualLayers.MaintenancePanel, comp.OpenPanelSpriteState));
        door.ClosedSpriteStates.Add((WiresVisualLayers.MaintenancePanel, comp.OpenPanelSpriteState));

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningPanelSpriteState, 0f) },
        });

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingPanelSpriteState, 0f) },
        });
    }

    private void OnAppearanceChange(EntityUid uid, AirlockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<DoorState>(DoorVisuals.State, out var state))
            state = DoorState.Closed;

        args.TryGetData<bool>(PowerDeviceVisuals.Powered, out var hasPower);

        var showBaseUnlit = false;
        var showBolted = false;
        var showEmergency = false;

        if (hasPower)
        {
            args.TryGetData<bool>(DoorVisuals.BoltLights, out var boltedVisible);
            showBolted = boltedVisible && (state == DoorState.Closed || state == DoorState.Welded);

            args.TryGetData<bool>(DoorVisuals.EmergencyLights, out var emergencyVisible);
            showEmergency = emergencyVisible;

            if (!showBolted && !showEmergency)
            {
                if (state == DoorState.Closing || state == DoorState.Opening || state == DoorState.Denying || state == DoorState.Closed)
                    showBaseUnlit = true;

                if (state == DoorState.Open && comp.OpenUnlitVisible)
                    showBaseUnlit = true;

                args.TryGetData<bool>(DoorVisuals.ClosedLights, out var closedLightsVisible);
                if (state == DoorState.Closed && closedLightsVisible)
                    showBaseUnlit = true;
            }
        }

        _sprite.LayerSetVisible((uid, args.Sprite), DoorVisualLayers.BaseUnlit, showBaseUnlit);
        _sprite.LayerSetVisible((uid, args.Sprite), DoorVisualLayers.BaseBolted, showBolted);
        if (comp.EmergencyAccessLayer)
        {
            var isDoorIdle = state != DoorState.Open && state != DoorState.Opening && state != DoorState.Closing;
            _sprite.LayerSetVisible((uid, args.Sprite), DoorVisualLayers.BaseEmergencyAccess,
                showEmergency && isDoorIdle && !showBolted);
        }

        if (comp.OpenUnlitVisible)
        {
            switch (state)
            {
                case DoorState.Open:
                    _sprite.LayerSetRsiState((uid, args.Sprite), DoorVisualLayers.BaseUnlit, comp.OpenSpriteState);
                    break;
                case DoorState.Closed:
                    _sprite.LayerSetRsiState((uid, args.Sprite), DoorVisualLayers.BaseUnlit, comp.ClosedSpriteState);
                    break;
            }
        }
    }
}
