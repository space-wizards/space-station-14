using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed partial class DoorSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    private void InitializeClientAirlock()
    {
        SubscribeLocalEvent<AirlockComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AirlockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentStartup(Entity<AirlockComponent> airlock, ref ComponentStartup args)
    {
        // Has to be on component startup because we don't know what order components initialize in and running this before DoorComponent inits _will_ crash.
        if (!TryComp<DoorComponent>(airlock, out var door))
            return;

        if (airlock.Comp
            .OpenUnlitVisible) // Otherwise there are flashes of the fallback sprite between clicking on the door and the door closing animation starting.
        {
            door.OpenSpriteStates.Add((DoorVisualLayers.BaseUnlit, airlock.Comp.OpenSpriteState));
            door.ClosedSpriteStates.Add((DoorVisualLayers.BaseUnlit, airlock.Comp.ClosedSpriteState));
        }

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
            {
                LayerKey = DoorVisualLayers.BaseUnlit,
                KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.OpeningSpriteState, 0f)},
            }
        );

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
            {
                LayerKey = DoorVisualLayers.BaseUnlit,
                KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.ClosingSpriteState, 0f)},
            }
        );

        door.DenyingAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(airlock.Comp.DenyAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.DenySpriteState, 0f)},
                }
            }
        };

        if (!airlock.Comp.AnimatePanel)
            return;

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick()
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.OpeningPanelSpriteState, 0f)},
        });

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.ClosingPanelSpriteState, 0f)},
        });
    }

    private void OnAppearanceChange(Entity<AirlockComponent> airlock, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var boltedVisible = false;
        var emergencyLightsVisible = false;
        var unlitVisible = false;

        if (!_appearanceSystem.TryGetData<DoorState>(airlock, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (_appearanceSystem.TryGetData<bool>(airlock, DoorVisuals.Powered, out var powered, args.Component) && powered)
        {
            boltedVisible =
                _appearanceSystem.TryGetData<bool>(airlock, DoorVisuals.BoltLights, out var lights, args.Component)
                && lights && state is DoorState.Closed or DoorState.WeldedClosed;

            emergencyLightsVisible =
                _appearanceSystem.TryGetData<bool>(airlock,
                    DoorVisuals.EmergencyLights,
                    out var eaLights,
                    args.Component) && eaLights;
            unlitVisible =
                (state is DoorState.AttemptingCloseBySelf
                     or DoorState.AttemptingOpenBySelf
                     or DoorState.AttemptingCloseByPrying
                     or DoorState.AttemptingOpenByPrying
                     or DoorState.Denying
                 || state == DoorState.Open && airlock.Comp.OpenUnlitVisible
                 || _appearanceSystem.TryGetData<bool>(airlock,
                     DoorVisuals.ClosedLights,
                     out var closedLights,
                     args.Component) && closedLights)
                && !boltedVisible && !emergencyLightsVisible;
        }

        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);

        if (airlock.Comp.EmergencyAccessLayer)
        {
            args.Sprite.LayerSetVisible(
                DoorVisualLayers.BaseEmergencyAccess,
                emergencyLightsVisible
                && state != DoorState.Open
                && state != DoorState.AttemptingOpenBySelf
                && state != DoorState.AttemptingCloseBySelf
                && !boltedVisible
            );
        }

        switch (state)
        {
            case DoorState.Open:
                args.Sprite.LayerSetState(DoorVisualLayers.BaseUnlit, airlock.Comp.ClosingSpriteState);
                args.Sprite.LayerSetAnimationTime(DoorVisualLayers.BaseUnlit, 0);

                break;
            case DoorState.Closed:
                args.Sprite.LayerSetState(DoorVisualLayers.BaseUnlit, airlock.Comp.OpeningSpriteState);
                args.Sprite.LayerSetAnimationTime(DoorVisualLayers.BaseUnlit, 0);

                break;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.Closing:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.Opening:
            case DoorState.WeldedClosed:
            case DoorState.Denying:
            case DoorState.Emagging:
            default:
                break;
        }
    }
}
