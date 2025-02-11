using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Content.Shared.Power;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors.Systems;

/// <summary>
/// Handles airlock-specific client-side door behaviour.
/// </summary>
public sealed partial class DoorSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    private void InitializeClientAirlock()
    {
        SubscribeLocalEvent<AirlockComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AirlockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    #region Visuals and Animations

    private void OnComponentStartup(Entity<AirlockComponent> airlock, ref ComponentStartup args)
    {
        // Has to be on component startup because we don't know what order components initialize in and running this
        // before DoorComponent inits _will_ crash.
        if (!TryComp<DoorComponent>(airlock, out var door))
            return;

        if (airlock.Comp.OpenUnlitVisible) // Otherwise there are flashes of the fallback sprite between clicking on the
                                           // door and the door closing animation starting.
        {
            door.OpenSpriteStates.Add((DoorVisualLayers.BaseUnlit, airlock.Comp.OpenSpriteState));
            door.ClosedSpriteStates.Add((DoorVisualLayers.BaseUnlit, airlock.Comp.ClosedSpriteState));
        }

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
            {
                LayerKey = DoorVisualLayers.BaseUnlit,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.OpeningSpriteState, 0f),
                },
            }
        );

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
            {
                LayerKey = DoorVisualLayers.BaseUnlit,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.ClosingSpriteState, 0f),
                },
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
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.DenySpriteState, 0f) },
                },
            },
        };

        if (!airlock.Comp.AnimatePanel)
            return;

        ((Animation)door.OpeningAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.OpeningPanelSpriteState, 0f) },
        });

        ((Animation)door.ClosingAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick
        {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(airlock.Comp.ClosingPanelSpriteState, 0f) },
        });
    }

    /// <summary>
    /// Handles animating the airlock. Airlocks have extra animation on top of regular door animation.
    /// </summary>
    private void OnAppearanceChange(Entity<AirlockComponent> airlock, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp<DoorComponent>(airlock, out var door))
            return;

        // Airlocks have three kinds of lights: bolted, emergency and regular.
        var boltedVisible = false;
        var emergencyLightsVisible = false;
        var unlitVisible = false;

        // Determine which lights should be switched on.
        if (_appearanceSystem.TryGetData<bool>(airlock, PowerDeviceVisuals.Powered, out var powered, args.Component) &&
            powered)
        {
            boltedVisible = IsBoltedVisible(airlock, args.Component, door.State);
            emergencyLightsVisible = IsEmergencyLightsVisible(airlock, args.Component);
            unlitVisible = IsUnlitVisible(airlock, args.Component, door.State) && !boltedVisible &&
                           !emergencyLightsVisible;
        }

        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);

        // If the airlock has a sprite for emergency access lights, set the layer's visibility.
        if (airlock.Comp.EmergencyAccessLayer)
        {
            // Emergency lights are visible when the door is fully closed.
            var visible = emergencyLightsVisible
                          && door.State is DoorState.Closed or DoorState.Welded
                          && !boltedVisible;

            args.Sprite.LayerSetVisible(DoorVisualLayers.BaseEmergencyAccess, visible);
        }

        switch (door.State)
        {
            case DoorState.Open:
                args.Sprite.LayerSetState(DoorVisualLayers.BaseUnlit, airlock.Comp.ClosingSpriteState);

                break;
            case DoorState.Closed:
                args.Sprite.LayerSetState(DoorVisualLayers.BaseUnlit, airlock.Comp.OpeningSpriteState);

                break;
        }
    }

    private bool IsUnlitVisible(Entity<AirlockComponent> airlock,
        AppearanceComponent comp,
        DoorState state)
    {
        return state is DoorState.Open && airlock.Comp.OpenUnlitVisible
               // Unlit is not visible when being pried open or closed.
               || state is DoorState.AttemptingCloseBySelf
                   or DoorState.AttemptingOpenBySelf
                   or DoorState.Opening
                   or DoorState.Closing
                   or DoorState.Denying
               || _appearanceSystem.TryGetData<bool>(airlock, DoorVisuals.ClosedLights, out var closedLights, comp)
               && closedLights;
    }

    private bool IsEmergencyLightsVisible(Entity<AirlockComponent> airlock, AppearanceComponent comp)
    {
        return _appearanceSystem.TryGetData<bool>(airlock,
                   DoorVisuals.EmergencyLights,
                   out var visible,
                   comp)
               && visible;
    }

    private bool IsBoltedVisible(Entity<AirlockComponent> airlock, AppearanceComponent comp, DoorState doorState)
    {
        return doorState is DoorState.Closed or DoorState.Welded
               && _appearanceSystem.TryGetData<bool>(airlock, DoorVisuals.BoltLights, out var visible, comp)
               && visible;
    }

    #endregion

}
