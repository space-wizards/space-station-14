using System;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class FlashLightVisualizer : AppearanceVisualizer
    {
        private string _powerSource;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("PowerSource", out var powerSource))
            {
                _powerSource = powerSource.AsString();
            }
        }

        private Animation radiatingLightAnimation = new Animation
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

        Action<string> RadiatingCallback;

        private void RadiatingLightAnimationCallback(string s, AnimationPlayerComponent ap, Animation animation)
        {
            ap.Play(animation, s);
        }

        private Animation blinkingLightAnimation = new Animation
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

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData(HandheldLightVisuals.Power,
                out HandheldLightPowerStates state))
            {
                PlayAnimation(component, state);
            }
        }

        public void PlayAnimation(AppearanceComponent component, HandheldLightPowerStates state)
        {
            component.Owner.EnsureComponent(out AnimationPlayerComponent animationPlayer);

            switch (state)
            {
                case HandheldLightPowerStates.LowPower:
                    if (!animationPlayer.HasRunningAnimation("radiatingLight"))
                    {
                        animationPlayer.Play(radiatingLightAnimation, "radiatingLight");
                        RadiatingCallback = (s) => animationPlayer.Play(radiatingLightAnimation, s);
                        animationPlayer.AnimationCompleted += RadiatingCallback;
                    }

                    break;
                case HandheldLightPowerStates.Dying:
                    animationPlayer.Stop("radiatingLight");
                    animationPlayer.AnimationCompleted -= RadiatingCallback;
                    if (!animationPlayer.HasRunningAnimation("blinkingLight"))
                    {
                        animationPlayer.Play(blinkingLightAnimation, "blinkingLight");
                        animationPlayer.AnimationCompleted +=
                            s => animationPlayer.Play(blinkingLightAnimation, s);
                    }
                    break;
                default:
                    if (animationPlayer.HasRunningAnimation("blinkingLight"))
                    {
                        animationPlayer.Stop("blinkingLight");
                    }

                    if (animationPlayer.HasRunningAnimation("radiatingLight"))
                    {
                        animationPlayer.Stop("radiatingLight");
                    }

                    break;
            }
        }
    }
}
