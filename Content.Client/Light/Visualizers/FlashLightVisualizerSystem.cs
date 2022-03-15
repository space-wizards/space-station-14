using System;
using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public sealed class FlashLightVisualizerSystem : EntitySystem
    {
        private static readonly Animation RadiatingLightAnimation = new()
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

        private static readonly Animation BlinkingLightAnimation = new()
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

        [Dependency] private readonly AnimationPlayerSystem _player = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HandheldLightComponent, AppearanceChangeEvent>(OnChangeData);
            SubscribeLocalEvent<HandheldLightComponent, AnimationCompletedEvent>(OnAnimComplete);
        }

        private void OnChangeData(EntityUid uid, HandheldLightComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Component.TryGetData(HandheldLightVisuals.Power,
                    out HandheldLightPowerStates state))
            {
                PlayAnimation(args.Component, state);
            }
        }

        private void OnAnimComplete(EntityUid uid, HandheldLightComponent component, AnimationCompletedEvent args)
        {
            switch (args.Key)
            {
                case "radiationLight":
                    _player.Play(args.Uid, RadiatingLightAnimation, "radiatingLight");
                    break;
                case "blinkingLight":
                    _player.Play(args.Uid, BlinkingLightAnimation, "blinkingLight");
                    break;
            }
        }

        private void PlayAnimation(AppearanceComponent component, HandheldLightPowerStates state)
        {
            var animationPlayer = EnsureComp<AnimationPlayerComponent>(component.Owner);

            switch (state)
            {
                case HandheldLightPowerStates.LowPower:
                    _player.Stop(animationPlayer, "blinkingLight");
                    if (!_player.HasRunningAnimation(animationPlayer, "radiatingLight"))
                        _player.Play(animationPlayer, RadiatingLightAnimation, "radiatingLight");
                    break;
                case HandheldLightPowerStates.Dying:
                    _player.Stop(animationPlayer, "radiatingLight");
                    if (!_player.HasRunningAnimation(animationPlayer, "blinkingLight"))
                        _player.Play(animationPlayer, BlinkingLightAnimation, "blinkingLight");
                    break;
                case HandheldLightPowerStates.FullPower:
                    _player.Stop(animationPlayer, "blinkingLight");
                    _player.Stop(animationPlayer, "radiatingLight");
                    break;
            }
        }
    }
}
