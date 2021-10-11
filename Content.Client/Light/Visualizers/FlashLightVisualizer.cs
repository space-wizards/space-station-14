using System;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public class FlashLightVisualizer : AppearanceVisualizer
    {
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

        private Action<string>? _radiatingCallback;
        private Action<string>? _blinkingCallback;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData(HandheldLightVisuals.Power,
                out HandheldLightPowerStates state))
            {
                PlayAnimation(component, state);
            }
        }

        private void PlayAnimation(AppearanceComponent component, HandheldLightPowerStates state)
        {
            component.Owner.EnsureComponent(out AnimationPlayerComponent animationPlayer);

            switch (state)
            {
                case HandheldLightPowerStates.LowPower:
                    if (!animationPlayer.HasRunningAnimation("radiatingLight"))
                    {
                        animationPlayer.Play(_radiatingLightAnimation, "radiatingLight");
                        _radiatingCallback = (s) => animationPlayer.Play(_radiatingLightAnimation, s);
                        animationPlayer.AnimationCompleted += _radiatingCallback;
                    }

                    break;
                case HandheldLightPowerStates.Dying:
                    animationPlayer.Stop("radiatingLight");
                    animationPlayer.AnimationCompleted -= _radiatingCallback;
                    if (!animationPlayer.HasRunningAnimation("blinkingLight"))
                    {
                        animationPlayer.Play(_blinkingLightAnimation, "blinkingLight");
                        _blinkingCallback = (s) => animationPlayer.Play(_blinkingLightAnimation, s);
                        animationPlayer.AnimationCompleted += _blinkingCallback;
                    }

                    break;
                case HandheldLightPowerStates.FullPower:
                    if (animationPlayer.HasRunningAnimation("blinkingLight"))
                    {
                        animationPlayer.Stop("blinkingLight");
                        animationPlayer.AnimationCompleted -= _blinkingCallback;
                    }

                    if (animationPlayer.HasRunningAnimation("radiatingLight"))
                    {
                        animationPlayer.Stop("radiatingLight");
                        animationPlayer.AnimationCompleted -= _radiatingCallback;
                    }

                    break;
            }
        }
    }
}
