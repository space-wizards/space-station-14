using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices.ComTypes;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Namotion.Reflection;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class RadiatingLightVisualizer : AppearanceVisualizer
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

        private void RadiatingLightAnimationCallback(string s, AnimationPlayerComponent ap, Animation animation) {
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

        private void BlinkingLightAnimationCallback(string s, AnimationPlayerComponent ap, Animation animation) {
            ap.Play(animation, s);
        }

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

            switch (_powerSource)
            {
                case "oil":
                    if (!animationPlayer.HasRunningAnimation("radiatingLight"))
                    {
                        animationPlayer.Play(radiatingLightAnimation, "radiatingLight");
                        RadiatingCallback = (s) => animationPlayer.Play(radiatingLightAnimation, s);
                        animationPlayer.AnimationCompleted += s => animationPlayer.Play(radiatingLightAnimation, s);
                    }

                    break;
                case "electrical":
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
                                    s =>
                                    {
                                        if (!animationPlayer.HasRunningAnimation("blinkingLight"))
                                        {
                                            animationPlayer.Play(blinkingLightAnimation, s);
                                        }
                                    };
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

                    break;
                default:
                    break;
            }
        }
    }
}
