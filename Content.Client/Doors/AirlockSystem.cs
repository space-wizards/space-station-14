using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Doors;

public sealed class AirlockSystem : SharedAirlockSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AirlockComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(EntityUid uid, AirlockComponent comp, ComponentInit args)
    {
        comp.OpenAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.OpenCloseAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningSpriteState, 0f) },
                }
            },
        };

        comp.CloseAnimation = new Animation()
        {
            Length = TimeSpan.FromSeconds(comp.OpenCloseAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingSpriteState, 0f) },
                }
            },
        };

        comp.DenyAnimation = new Animation()
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

        if(!comp.AnimatePanel)
            return;

        ((Animation)comp.OpenAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick() {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningPanelSpriteState, 0f)},
        });
        
        ((Animation)comp.CloseAnimation).AnimationTracks.Add(new AnimationTrackSpriteFlick() {
            LayerKey = WiresVisualLayers.MaintenancePanel,
            KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingPanelSpriteState, 0f)},
        });
    }

    private void OnAppearanceChange(EntityUid uid, AirlockComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var boltedVisible = false;
        var emergencyLightsVisible = false;
        var unlitVisible = false;

        if (!_appearanceSystem.TryGetData<DoorState>(uid, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        TryComp<DoorComponent>(uid, out var door);
        TryComp<AnimationPlayerComponent>(uid, out var animPlayer);
        if (_animationSystem.HasRunningAnimation(uid, animPlayer, AirlockComponent.AnimationKey))
            _animationSystem.Stop(uid, animPlayer, AirlockComponent.AnimationKey);
        
        switch(state)
        {
            case DoorState.Open:
                if (comp.OpenUnlitVisible)
                    args.Sprite.LayerSetState(DoorVisualLayers.BaseUnlit, comp.OpenSpriteState);
                break;
            case DoorState.Closed:
                args.Sprite.LayerSetState(DoorVisualLayers.BaseUnlit, comp.ClosedSpriteState);
                args.Sprite.LayerSetState(DoorVisualLayers.BaseBolted, comp.BoltedSpriteState);
                break;
            case DoorState.Opening:
                if (animPlayer != null)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.OpenAnimation, AirlockComponent.AnimationKey);
                break;
            case DoorState.Closing:
                if (door != null && door.CurrentlyCrushing.Count == 0 && animPlayer != null)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.CloseAnimation, AirlockComponent.AnimationKey);
                else
                    goto case DoorState.Closed;
                break;
            case DoorState.Denying:
                if (animPlayer != null)
                    _animationSystem.Play(uid, animPlayer, (Animation)comp.DenyAnimation, AirlockVisualizerComponent.AnimationKey);
                break;
            case DoorState.Emagging:
            case DoorState.Welded:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_appearanceSystem.TryGetData<bool>(uid, DoorVisuals.Powered, out var powered, args.Component) && powered)
        {
            boltedVisible = _appearanceSystem.TryGetData<bool>(uid, DoorVisuals.BoltLights, out var lights, args.Component) && lights;
            emergencyLightsVisible = _appearanceSystem.TryGetData<bool>(uid, DoorVisuals.EmergencyLights, out var eaLights, args.Component) && eaLights;
            unlitVisible =
                    state == DoorState.Closing
                ||  state == DoorState.Opening
                ||  state == DoorState.Denying
                || (state == DoorState.Open && comp.OpenUnlitVisible)
                || (_appearanceSystem.TryGetData<bool>(uid, DoorVisuals.ClosedLights, out var closedLights, args.Component) && closedLights);
        }

        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
        args.Sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
        if (comp.EmergencyAccessLayer)
        {
            args.Sprite.LayerSetVisible(
                DoorVisualLayers.BaseEmergencyAccess,
                    emergencyLightsVisible
                &&  state != DoorState.Open
                &&  state != DoorState.Opening
                &&  state != DoorState.Closing
            );
        }
    }
}
