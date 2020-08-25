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
        protected Dictionary<string, LightBehaviourData> BehaviourData = new Dictionary<string, LightBehaviourData>();

        private int _prevColorIndex = default;
        private int _currentColorIndex = default;
        private float _originalRadius = default;
        private float _originalEnergy = default;
        private Color _originalColor = default;
        private bool _originalEnabled = default;
        private bool _startEnabled = false;

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
                                    new AnimationTrackProperty.KeyFrame((data.MaxValue + data.MinValue) * 0.5f, 0),
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue, (float)seconds * 0.25f),
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, (float)seconds * 0.75f),
                                    new AnimationTrackProperty.KeyFrame((data.MaxValue + data.MinValue) * 0.5f, (float)seconds)
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
                                    new AnimationTrackProperty.KeyFrame((data.MaxValue + data.MinValue) * 0.5f, 0),
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue, (float)seconds * 0.25f),
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, (float)seconds * 0.75f),
                                    new AnimationTrackProperty.KeyFrame((data.MaxValue + data.MinValue) * 0.5f, (float)seconds)
                                }
                            }
                        }
                    };

                case LightBehaviourType.RandomSize: // todo: we don't need animator for this anymore

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
                                    new AnimationTrackProperty.KeyFrame(false, MathF.Min(data.MaxValue, (float)seconds))
                                }
                            }
                        }
                    };

                case LightBehaviourType.ColorSequence: // todo: we don't need animator for this anymore

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

            var playerComponent = Owner.EnsureComponent<AnimationPlayerComponent>();
            playerComponent.AnimationCompleted += (key => PlayNewAnimation(playerComponent, key));

            CopyLightSettings();

            if (_startEnabled)
            {
                StartLightBehaviour(); 
            }
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
        /// Start animating a light behaviour with the specified ID. If the specified ID is empty, it will start animating all light behaviour entries.
        /// If specified light behaviours are already animating or if the owning entity has no PointLightComponent, calling this does nothing.
        /// </summary>
        public void StartLightBehaviour(string id = "")
        {
            if (!Owner.HasComponent<PointLightComponent>())
            {
                return;
            }

            _currentColorIndex = 1;
            _prevColorIndex = 0;
            var playerComponent = Owner.EnsureComponent<AnimationPlayerComponent>();

            foreach (KeyValuePair<string, LightBehaviourData> pair in BehaviourData)
            {
                if (pair.Value.ID != id && id != string.Empty)
                {
                    continue;
                }

                if ((pair.Value.LightBehaviourType == LightBehaviourType.ColorSequence ||
                    pair.Value.LightBehaviourType == LightBehaviourType.ColorSequenceSmooth) &&
                    pair.Value.ColorsToCycle.Count < 2)
                {
                    Logger.Warning($"{Owner.Name} has a color cycling {nameof(LightBehaviourComponent)} with less than 2 colors!");
                    pair.Value.ColorsToCycle.Add(Color.Red);
                    pair.Value.ColorsToCycle.Add(Color.Blue);
                }

                if (!playerComponent.HasRunningAnimation(pair.Key))
                {
                    var animation = GenerateAnimation(pair.Value);
                    playerComponent.Play(animation, pair.Key);
                }
            }
        }

        /// <summary>
        /// If the light behaviour with the specified ID is animating, then stop it.
        /// If no ID is specified then all light behaviours will be stopped.
        /// </summary>
        public void StopLightBehaviour(string id = "", bool removeBehaviourData = false, bool resetToOriginalSettings = false)
        {
            if (Owner.TryGetComponent<AnimationPlayerComponent>(out var playerComponent))
            {
                var toRemove = new List<string>();

                foreach (KeyValuePair<string, LightBehaviourData> pair in BehaviourData)
                {
                    if (playerComponent.HasRunningAnimation(pair.Key) && (pair.Value.ID == id || id == string.Empty))
                    {
                        playerComponent.Stop(pair.Key);
                        toRemove.Add(pair.Key);
                    }
                }

                if (removeBehaviourData)
                {
                    foreach (var key in toRemove)
                    {
                        BehaviourData.Remove(key);
                    }
                }

                if (resetToOriginalSettings && Owner.TryGetComponent<PointLightComponent>(out var pointLight))
                {
                    pointLight.Color = _originalColor;
                    pointLight.Enabled = _originalEnabled;
                    pointLight.Energy = _originalEnergy;
                    pointLight.Radius = _originalRadius;
                }
            }
        }

        /// <summary>
        /// Add a new light behaviour to the component and start it immediately unless otherwise specified.
        /// </summary>
        public void AddNewLightBehaviour(LightBehaviourData data, bool playImmediately = true)
        {
            int key = 0;

            while (BehaviourData.TryGetValue(key.ToString(), out _))
            {
                key++;
            }

            BehaviourData[key.ToString()] = data;

            if (playImmediately)
            {
                StartLightBehaviour(data.ID);
            }
        }

        private void PlayNewAnimation(AnimationPlayerComponent playerComponent, string key)
        {
            var animation = GenerateAnimation(BehaviourData[key]);
            playerComponent.Play(animation, key);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => _startEnabled, "enabled", false);
            var list = serializer.ReadDataField("behaviours", new List<LightBehaviourData>());
            int idx = 0;

            foreach (var behaviour in list)
            {
                BehaviourData[idx.ToString()] = behaviour;
                idx++;
            }
        }
    }
}
