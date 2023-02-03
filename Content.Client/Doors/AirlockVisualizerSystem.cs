using Content.Client.Wires.Visualizers;
using Content.Shared.Doors.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;

namespace Content.Client.Doors;

public sealed class AirlockVisualizerSystem : VisualizerSystem<AirlockVisualizerComponent>
{
    #region Dependencies
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirlockVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, AirlockVisualizerComponent comp, ComponentInit args)
    {
        comp.CloseAnimation = new Animation { Length = TimeSpan.FromSeconds(comp.Delay) };
        {
            var flick = new AnimationTrackSpriteFlick();
            comp.CloseAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DoorVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));

            if (!comp.SimpleVisuals)
            {
                var flickUnlit = new AnimationTrackSpriteFlick();
                comp.CloseAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing_unlit", 0f));

                if (comp.AnimatedPanel)
                {
                    var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                    comp.CloseAnimation.AnimationTracks.Add(flickMaintenancePanel);
                    flickMaintenancePanel.LayerKey = WiresVisualLayers.MaintenancePanel;
                    flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_closing", 0f));
                }
            }
        }

        comp.OpenAnimation = new Animation { Length = TimeSpan.FromSeconds(comp.Delay) };
        {
            var flick = new AnimationTrackSpriteFlick();
            comp.OpenAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = DoorVisualLayers.Base;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));

            if (!comp.SimpleVisuals)
            {
                var flickUnlit = new AnimationTrackSpriteFlick();
                comp.OpenAnimation.AnimationTracks.Add(flickUnlit);
                flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
                flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening_unlit", 0f));

                if (comp.AnimatedPanel)
                {
                    var flickMaintenancePanel = new AnimationTrackSpriteFlick();
                    comp.OpenAnimation.AnimationTracks.Add(flickMaintenancePanel);
                    flickMaintenancePanel.LayerKey = WiresVisualLayers.MaintenancePanel;
                    flickMaintenancePanel.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("panel_opening", 0f));
                }
            }
        }

        comp.EmaggingAnimation = new Animation { Length = TimeSpan.FromSeconds(comp.Delay) };
        {
            var flickUnlit = new AnimationTrackSpriteFlick();
            comp.EmaggingAnimation.AnimationTracks.Add(flickUnlit);
            flickUnlit.LayerKey = DoorVisualLayers.BaseUnlit;
            flickUnlit.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("sparks", 0f));
        }

        if (!comp.SimpleVisuals)
        {
            comp.DenyAnimation = new Animation { Length = TimeSpan.FromSeconds(comp.DenyDelay) };
            {
                var flick = new AnimationTrackSpriteFlick();
                comp.DenyAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = DoorVisualLayers.BaseUnlit;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny_unlit", 0f));
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, AirlockVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        // only start playing animations once.
        if (!_gameTiming.IsFirstTimePredicted)
            return;
        var sprite = args.Sprite;
        if (sprite == null)
            return;
        if(!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;
        
        if (!AppearanceSystem.TryGetData(uid, DoorVisuals.State, out DoorState state, args.Component))
            state = DoorState.Closed;

        var door = Comp<DoorComponent>(uid);
        if (AppearanceSystem.TryGetData(uid, DoorVisuals.BaseRSI, out string baseRsi, args.Component))
        {
            if (!_resourceCache.TryGetResource<RSIResource>(SharedSpriteComponent.TextureRoot / baseRsi, out var res))
            {
                Logger.Error("Unable to load RSI '{0}'. Trace:\n{1}", baseRsi, Environment.StackTrace);
            }
            foreach (ISpriteLayer layer in sprite.AllLayers)
            {
                layer.Rsi = res?.RSI;
            }
        }

        if (AnimationSystem.HasRunningAnimation(uid, animPlayer, AirlockVisualizerComponent.AnimationKey))
            AnimationSystem.Stop(uid, animPlayer, AirlockVisualizerComponent.AnimationKey);

        switch (state)
        {
            case DoorState.Open:
                sprite.LayerSetState(DoorVisualLayers.Base, "open");
                if (comp.OpenUnlitVisible && !comp.SimpleVisuals)
                {
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "open_unlit");
                }
                break;
            case DoorState.Closed:
                sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                if (!comp.SimpleVisuals)
                {
                    sprite.LayerSetState(DoorVisualLayers.BaseUnlit, "closed_unlit");
                    sprite.LayerSetState(DoorVisualLayers.BaseBolted, "bolted_unlit");
                }
                break;
            case DoorState.Opening:
                AnimationSystem.Play(uid, animPlayer, comp.OpenAnimation, AirlockVisualizerComponent.AnimationKey);
                break;
            case DoorState.Closing:
                if (door.CurrentlyCrushing.Count == 0)
                    AnimationSystem.Play(uid, animPlayer, comp.CloseAnimation, AirlockVisualizerComponent.AnimationKey);
                else
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                break;
            case DoorState.Denying:
                AnimationSystem.Play(uid, animPlayer, comp.DenyAnimation, AirlockVisualizerComponent.AnimationKey);
                break;
            case DoorState.Welded:
                break;
            case DoorState.Emagging:
                AnimationSystem.Play(uid, animPlayer, comp.EmaggingAnimation, AirlockVisualizerComponent.AnimationKey);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (comp.SimpleVisuals)
            return;

        var boltedVisible = false;
        var emergencyLightsVisible = false;
        var unlitVisible = false;

        if (AppearanceSystem.TryGetData(uid, DoorVisuals.Powered, out bool powered, args.Component) && powered)
        {
            boltedVisible = AppearanceSystem.TryGetData(uid, DoorVisuals.BoltLights, out bool lights, args.Component) && lights;
            emergencyLightsVisible = AppearanceSystem.TryGetData(uid, DoorVisuals.EmergencyLights, out bool eaLights, args.Component) && eaLights;
            unlitVisible = state == DoorState.Closing
                || state == DoorState.Opening
                || state == DoorState.Denying
                || state == DoorState.Open && comp.OpenUnlitVisible
                || (AppearanceSystem.TryGetData(uid, DoorVisuals.ClosedLights, out bool closedLights, args.Component) && closedLights);
        }

        sprite.LayerSetVisible(DoorVisualLayers.BaseUnlit, unlitVisible);
        sprite.LayerSetVisible(DoorVisualLayers.BaseBolted, boltedVisible);
        if (comp.EmergencyAccessLayer)
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
