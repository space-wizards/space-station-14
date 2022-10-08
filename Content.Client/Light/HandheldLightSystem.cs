using Content.Client.Items;
using Content.Client.Light.Components;
using Content.Shared.Light;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Light;

public sealed class HandheldLightSystem : SharedHandheldLightSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    private const string RadiatingLightAnimationKey = "radiatingLight";
    private const string BlinkingLightAnimationKey = "blinkingLight";

    private readonly Animation _radiatingLightAnimation = new()
    {
        Length = TimeSpan.FromSeconds(1),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty
            {
                ComponentType = typeof(PointLightComponent),
                InterpolationMode = AnimationInterpolationMode.Linear,
                Property = nameof(PointLightComponent.Radius),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(3.0f, 0),
                    new AnimationTrackProperty.KeyFrame(2.0f, 0.5f),
                    new AnimationTrackProperty.KeyFrame(3.0f, 1)
                }
            }
        }
    };

    private readonly Animation _blinkingLightAnimation = new()
    {
        Length = TimeSpan.FromSeconds(1),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(PointLightComponent),
                //To create the blinking effect we go from nearly zero radius, to the light radius, and back
                //We do this instead of messing with the `PointLightComponent.enabled` because we don't want the animation to affect component behavior
                InterpolationMode = AnimationInterpolationMode.Nearest,
                Property = nameof(PointLightComponent.Radius),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(0.1f, 0),
                    new AnimationTrackProperty.KeyFrame(2f, 0.5f),
                    new AnimationTrackProperty.KeyFrame(0.1f, 1)
                }
            }
        }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldLightComponent, ItemStatusCollectMessage>(OnGetStatusControl);
        SubscribeLocalEvent<HandheldLightComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<HandheldLightComponent, AnimationCompletedEvent>(OnAnimCompleted);
    }

    private void OnAnimCompleted(EntityUid uid, HandheldLightComponent component, AnimationCompletedEvent args)
    {
        switch (args.Key)
        {
            case RadiatingLightAnimationKey:
                _animationPlayer.Play(uid, _radiatingLightAnimation, RadiatingLightAnimationKey);
                break;
            case BlinkingLightAnimationKey:
                _animationPlayer.Play(uid, _blinkingLightAnimation, BlinkingLightAnimationKey);
                break;
        }
    }

    private static void OnGetStatusControl(EntityUid uid, HandheldLightComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new HandheldLightStatus(component));
    }

    private void OnAppearanceChange(EntityUid uid, HandheldLightComponent? component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref component) || !TryComp<PointLightComponent>(uid, out var pointLightComponent))
        {
            return;
        }
        if (!args.Component.TryGetData(HandheldLightVisuals.Power,
                out HandheldLightPowerStates state))
        {
            return;
        }

        // A really ugly way to save the initial PointLightComponent radius, so that we can reset it if the state's set to FullPower again.
        component.OriginalRadius ??= pointLightComponent.Radius;

        switch (state)
        {
            case HandheldLightPowerStates.FullPower:
                _animationPlayer.Stop(uid, BlinkingLightAnimationKey);
                _animationPlayer.Stop(uid, RadiatingLightAnimationKey);
                if (component.OriginalRadius != null)
                {
                    pointLightComponent.Radius = component.OriginalRadius.Value;
                }
                break;

            case HandheldLightPowerStates.LowPower:
                _animationPlayer.Stop(uid, BlinkingLightAnimationKey);
                if (!_animationPlayer.HasRunningAnimation(uid, RadiatingLightAnimationKey))
                {
                    _animationPlayer.Play(uid, _radiatingLightAnimation, RadiatingLightAnimationKey);
                }
                break;

            case HandheldLightPowerStates.Dying:
                _animationPlayer.Stop(uid, RadiatingLightAnimationKey);
                if (!_animationPlayer.HasRunningAnimation(uid, BlinkingLightAnimationKey))
                {
                    _animationPlayer.Play(uid, _blinkingLightAnimation, BlinkingLightAnimationKey);
                }
                break;
        }

    }
}
