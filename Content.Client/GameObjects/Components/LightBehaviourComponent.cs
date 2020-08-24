using System;
using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Shared.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components
{
    /// <summary>
    /// A component which applies a specific behaviour to a PointLightComponent on its owner.
    /// </summary>
    [RegisterComponent]
    public class LightBehaviourComponent : SharedLightBehaviourComponent 
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadOnly)]
        protected List<LightBehaviourData> BehaviourData = default;

        private int _prevColorIndex = default;
        private int _currentColorIndex = default;
        private float _originalRadius = default;
        private float _originalEnergy = default;
        private Color _originalColor = default;
        private bool _originalEnabled = default;

        private Animation GenerateAnimation(LightBehaviourData data)
        {
            var seconds = (double)data.MaxDuration;

            if (data.MinDuration > 0f)
            {
                seconds = _random.NextDouble() * (data.MaxDuration - data.MinDuration) + data.MinDuration;
            }

            switch (data.LightBehaviourType)
            {
                default:
                case LightBehaviourType.PulseSize:

                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Radius),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, 0),
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue, (float)seconds * 0.5f),
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, (float)seconds)
                                }
                            }
                        }
                    };

                case LightBehaviourType.PulseBrightness:

                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Energy),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, 0),
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue, (float)seconds * 0.5f),
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, (float)seconds)
                                }
                            }
                        }
                    };

                case LightBehaviourType.RandomSize:

                    var size = _random.NextDouble() * (data.MaxValue - data.MinValue) + data.MinValue;
                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Radius),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame((float)size, 0)
                                }
                            }
                        }
                    };

                case LightBehaviourType.RandomBrightness:

                    var brightness = _random.NextDouble() * (data.MaxValue - data.MinValue) + data.MinValue;
                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Energy),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame((float)brightness, 0)
                                }
                            }
                        }
                    };

                case LightBehaviourType.Flicker:

                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Enabled),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame(true, 0),
                                    new AnimationTrackProperty.KeyFrame(false, 0.05f)
                                }
                            }
                        }
                    };

                case LightBehaviourType.Toggle:

                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Enabled),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame(false, 0),
                                    new AnimationTrackProperty.KeyFrame(true, (float)seconds * 0.5f)
                                }
                            }
                        }
                    };

                case LightBehaviourType.ColorSequence:

                    _currentColorIndex = (_currentColorIndex + 1 >= data.ColorsToCycle.Count) ? 0 : _currentColorIndex + 1;
                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Color),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame(data.ColorsToCycle[_currentColorIndex], 0)
                                }
                            }
                        }
                    };

                case LightBehaviourType.ColorSequenceSmooth:

                    _prevColorIndex = _currentColorIndex;
                    _currentColorIndex = (_currentColorIndex + 1 >= data.ColorsToCycle.Count) ? 0 : _currentColorIndex + 1;
                    return new Animation
                    {
                        Length = TimeSpan.FromSeconds(seconds),
                        AnimationTracks =
                        {
                            new AnimationTrackComponentProperty
                            {
                                ComponentType = typeof(PointLightComponent),
                                InterpolationMode = AnimationInterpolationMode.Linear,
                                Property = nameof(PointLightComponent.Color),
                                KeyFrames =
                                {
                                    new AnimationTrackProperty.KeyFrame(data.ColorsToCycle[_prevColorIndex], 0),
                                    new AnimationTrackProperty.KeyFrame(data.ColorsToCycle[_currentColorIndex], (float)seconds)
                                }
                            }
                        }
                    };
                
            }
        }

        protected override void Startup()
        {
            base.Startup();

            CopyLightSettings();
            StartLightBehaviour(); //this is for testing
        }

        private void CopyLightSettings()
        {
            if (Owner.TryGetComponent<PointLightComponent>(out var pointLight))
            {
                _originalColor = pointLight.Color;
                _originalEnabled = pointLight.Enabled;
                _originalEnergy = pointLight.Energy;
                _originalRadius = pointLight.Radius;
            }
            else
            {
                // This component is useless without a point light, so maybe we should combine it with that component? Needs discussion
                Logger.Warning($"{Owner.Name} has a {nameof(LightBehaviourComponent)} but it has no {nameof(PointLightComponent)}! Check the prototype!");
            }
        }

        /// <summary>
        /// If the light behaviour isn't animating, then start animating. If it's already animating or if the owning entity has no PointLightComponent, calling this does nothing.
        /// </summary>
        public void StartLightBehaviour()
        {
            if (!Owner.HasComponent<PointLightComponent>())
            {
                return;
            }

            _currentColorIndex = 1;
            _prevColorIndex = 0;
            var playerComponent = Owner.EnsureComponent<AnimationPlayerComponent>();
            var animationCount = 0;

            foreach (LightBehaviourData data in BehaviourData)
            {
                if ((data.LightBehaviourType == LightBehaviourType.ColorSequence ||
                    data.LightBehaviourType == LightBehaviourType.ColorSequenceSmooth) &&
                    data.ColorsToCycle.Count < 2)
                {
                    Logger.Warning($"{Owner.Name} has a color cycling {nameof(LightBehaviourComponent)} with less than 2 colors! Check the prototype!");
                    data.ColorsToCycle.Add(Color.Red);
                    data.ColorsToCycle.Add(Color.Blue);
                }

                var key = nameof(LightBehaviourComponent) + animationCount;

                if (!playerComponent.HasRunningAnimation(key))
                {
                    var animation = GenerateAnimation(data);

                    playerComponent.Play(animation, key);
                    playerComponent.AnimationCompleted += s => PlayNewAnimation(playerComponent, data, s);
                }

                animationCount++;
            }
        }

        /// <summary>
        /// If the light behaviour is animating, then stop it and reset the light values to its original settings from the prototype.
        /// </summary>
        public void StopLightBehaviour()
        {
            if (Owner.TryGetComponent<AnimationPlayerComponent>(out var playerComponent))
            {
                for (int i = 0; i < BehaviourData.Count; i++)
                {
                    var key = nameof(LightBehaviourComponent) + i;

                    if (playerComponent.HasRunningAnimation(key))
                    {
                        playerComponent.Stop(key);
                    }
                }

                if (Owner.TryGetComponent<PointLightComponent>(out var pointLight))
                {
                    pointLight.Color = _originalColor;
                    pointLight.Enabled = _originalEnabled;
                    pointLight.Energy = _originalEnergy;
                    pointLight.Radius = _originalRadius;
                }
            }
        }

        private void PlayNewAnimation(AnimationPlayerComponent playerComponent, LightBehaviourData data, string id)
        {
            var animation = GenerateAnimation(data);
            playerComponent.Play(animation, id);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            BehaviourData = serializer.ReadDataField("behaviours", new List<LightBehaviourData>());
        }
    }
}
