using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;

namespace Content.Client.Airlock;

public sealed class AirlockSystem : VisualizerSystem<AirlockVisualsComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private Animation _closeAnimation = default!;
    private Animation _openAnimation = default!;
    private Animation _denyAnimation = default!;
    private Animation _emaggingAnimation = default!;

    private const string AnimationKey = "airlock_animation";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, AirlockVisualsComponent component, ComponentInit args)
    {
        EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
        _closeAnimation = new Animation { Length = TimeSpan.FromSeconds(component.Delay) };
        {
            var flick = new AnimationTrackSpriteFlick();
            _closeAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DoorVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

            if (!component.SimpleVisuals)
            {
                var flickUnlit = new AnimationTrackSpriteFlick();
                _closeAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing_unlit", 0f));

                if (component.AnimatedPanel)
                {
                    var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                    _closeAnimation.AnimationTracks.Add(flickMaintenancePanel);
                    flickMaintenancePanel.LayerKey = WiresVisualLayers.MaintenancePanel;
                    flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_closing", 0f));
                }
            }
        }

        _openAnimation = new Animation { Length = TimeSpan.FromSeconds(component.Delay) };
        {
            var flick = new AnimationTrackSpriteFlick();
            _openAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DoorVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

            if (!component.SimpleVisuals)
            {
                var flickUnlit = new AnimationTrackSpriteFlick();
                _openAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening_unlit", 0f));

                if (component.AnimatedPanel)
                {
                    var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                    _openAnimation.AnimationTracks.Add(flickMaintenancePanel);
                    flickMaintenancePanel.LayerKey = WiresVisualLayers.MaintenancePanel;
                    flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_opening", 0f));
                }
            }
        }
        _emaggingAnimation = new Animation { Length = TimeSpan.FromSeconds(component.Delay) };
        {
            var flickUnlit = new AnimationTrackSpriteFlick();
            _emaggingAnimation.AnimationTracks.Add(flickUnlit);
            flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
            flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("sparks", 0f));
        }

        if (!component.SimpleVisuals)
        {
            _denyAnimation = new Animation { Length = TimeSpan.FromSeconds(component.DenyDelay) };
            {
                var flick = new AnimationTrackSpriteFlick();
                _denyAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.BaseUnlit;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny_unlit", 0f));
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, AirlockVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        // only start playing animations once.
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var sprite = args.Sprite;
        if (sprite == null)
            return;
        var animPlayer = EntityManager.GetComponent<AnimationPlayerComponent>(component.Owner);
        if (!AppearanceSystem.TryGetData(uid, DoorVisuals.State, out DoorState state))
        {
            state = DoorState.Closed;
        }

        var door = EntityManager.GetComponent<DoorComponent>(component.Owner);

        if (AppearanceSystem.TryGetData(uid, DoorVisuals.BaseRSI, out string baseRsi))
        {
            if (!_resourceCache.TryGetResource<RSIResource>(SharedSpriteComponent.TextureRoot / baseRsi, out var res))
            {
                Logger.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);
            }

            foreach (var layer in sprite.AllLayers)
            {
                layer.Rsi = res?.RSI;
            }
        }

        if (animPlayer.HasRunningAnimation(AnimationKey))
        {
            animPlayer.Stop(AnimationKey);
        }

        switch (state)
        {
            case DoorState.Open:
                sprite.LayerSetState(DoorVisualLayers.Base, "open");
                if (component.OpenUnlitVisible && !component.SimpleVisuals)
                {
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "open_unlit");
                }

                break;
            case DoorState.Closed:
                sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                if (!component.SimpleVisuals)
                {
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                    sprite.LayerSetState(DoorVisualLayers.BaseBolted, "bolted_unlit");
                }

                break;
            case DoorState.Opening:
                AnimationSystem.Play(uid, _openAnimation, AnimationKey);
                break;
            case DoorState.Closing:
                if (door.CurrentlyCrushing.Count == 0)
                    AnimationSystem.Play(uid, _closeAnimation, AnimationKey);
                else
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                break;
            case DoorState.Denying:
                AnimationSystem.Play(uid, _denyAnimation, AnimationKey);
                break;
            case DoorState.Welded:
                break;
            case DoorState.Emagging:
                AnimationSystem.Play(uid, _emaggingAnimation, AnimationKey);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (component.SimpleVisuals)
            return;

        var boltedVisible = false;
        var emergencyLightsVisible = false;
        var unlitVisible = false;

        if (AppearanceSystem.TryGetData(uid, DoorVisuals.Powered, out bool powered) && powered)
        {
            boltedVisible = AppearanceSystem.TryGetData(uid, DoorVisuals.BoltLights, out bool lights) && lights;
            emergencyLightsVisible = AppearanceSystem.TryGetData(uid, DoorVisuals.EmergencyLights, out bool eaLights) && eaLights;
            unlitVisible = state == DoorState.Closing
                           || state == DoorState.Opening
                           || state == DoorState.Denying
                           || state == DoorState.Open && component.OpenUnlitVisible
                           || (AppearanceSystem.TryGetData(uid, DoorVisuals.ClosedLights, out bool closedLights) && closedLights);
        }

        sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
        sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
        if (component.EmergencyAccessLayer)
        {
            sprite.LayerSetVisible(DoorVisualLayers.BaseEmergencyAccess,
                emergencyLightsVisible
                && state != DoorState.Open
                && state != DoorState.Opening
                && state != DoorState.Closing);
        }
    }
}

public enum DoorVisualLayers : byte
{
    Base,
    BaseUnlit,
    BaseBolted,
    BaseEmergencyAccess,
}
