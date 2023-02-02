using Content.Client.Trigger;
using Content.Shared.Trigger;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Explosion;

public sealed partial class TriggerSystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /*
     * Currently all of the appearance stuff is hardcoded for portable flashers
     * If you ever add mines it shouldn't be hard to tweak it slightly
     */

    private const string AnimKey = "proximity";

    private static readonly Animation _flasherAnimation = new Animation
    {
        Length = TimeSpan.FromSeconds(0.3f),
        AnimationTracks = {
            new AnimationTrackSpriteFlick
            {
                LayerKey = ProximityTriggerVisualLayers.Base,
                KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame("flashing", 0f)}
            },
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(PointLightComponent),
                InterpolationMode = AnimationInterpolationMode.Nearest,
                Property = nameof(PointLightComponent.Radius),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(0.1f, 0),
                    new AnimationTrackProperty.KeyFrame(3f, 0.1f),
                    new AnimationTrackProperty.KeyFrame(0.1f, 0.5f)
                }
            }
        }
    };

    private void InitializeProximity()
    {
        SubscribeLocalEvent<TriggerOnProximityComponent, ComponentInit>(OnProximityInit);
        SubscribeLocalEvent<TriggerOnProximityComponent, AppearanceChangeEvent>(OnProxAppChange);
        SubscribeLocalEvent<TriggerOnProximityComponent, AnimationCompletedEvent>(OnProxAnimation);
    }

    private void OnProxAnimation(EntityUid uid, TriggerOnProximityComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;

        // So animation doesn't get spammed if no server state comes in.
        _appearance.SetData(uid, ProximityTriggerVisualState.State, ProximityTriggerVisuals.Inactive, appearance);
        OnChangeData(uid, component, appearance);
    }

    private void OnProximityInit(EntityUid uid, TriggerOnProximityComponent component, ComponentInit args)
    {
        EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
    }

    private void OnProxAppChange(EntityUid uid, TriggerOnProximityComponent component, ref AppearanceChangeEvent args)
    {
        OnChangeData(uid, component, args.Component, args.Sprite);
    }

    private void OnChangeData(EntityUid uid, TriggerOnProximityComponent component, AppearanceComponent appearance, SpriteComponent? spriteComponent = null)
    {
        if (!Resolve(uid, ref spriteComponent))
            return;

        TryComp<AnimationPlayerComponent>(component.Owner, out var player);
        _appearance.TryGetData<ProximityTriggerVisuals>(appearance.Owner, ProximityTriggerVisualState.State, out var state, appearance);

        switch (state)
        {
            case ProximityTriggerVisuals.Inactive:
                // Don't interrupt the flash animation
                if (_player.HasRunningAnimation(uid, player, AnimKey)) return;
                _player.Stop(uid, player, AnimKey);
                spriteComponent.LayerSetState(ProximityTriggerVisualLayers.Base, "on");
                break;
            case ProximityTriggerVisuals.Active:
                if (_player.HasRunningAnimation(uid, player, AnimKey)) return;
                _player.Play(uid, player, _flasherAnimation, AnimKey);
                break;
            case ProximityTriggerVisuals.Off:
            default:
                _player.Stop(uid, player, AnimKey);
                spriteComponent.LayerSetState(ProximityTriggerVisualLayers.Base, "off");
                break;
        }
    }

    public enum ProximityTriggerVisualLayers : byte
    {
        Base,
    }
}
