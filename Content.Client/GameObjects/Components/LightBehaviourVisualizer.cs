using System;
using YamlDotNet.RepresentationModel;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using SharpFont;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    [Serializable]
    public class LightBehaviourVisualizer : AppearanceVisualizer
    {
        [Serializable]
        public enum LightBehaviourType
        {
            PulseBrightness,    // a light fading in and out smoothly
            PulseSize,          // a light getting bigger and smaller smoothly
            RandomBrightness,   // something like a campfire flickering
            RandomSize,         // sort of campfire-esque as well
            Flicker,            // light turns on then off again. 
            ColorSequence,      // light immediately changes colors using the predetermined sequence (or random if the sequence is empty)
            ColorSequenceSmooth // same as above but lerped
        }

        [Serializable]
        public struct LightBehaviourData : IExposeData
        {
            public string ID;
            public LightBehaviourType LightBehaviourType;
            public float MinValue;
            public float MaxValue;
            public float MinDuration;
            public float MaxDuration;
            public List<Color> ColorsToCycle;

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref ID, "id", string.Empty);
                serializer.DataField(ref LightBehaviourType, "type", LightBehaviourType.Flicker);
                serializer.DataField(ref MinValue, "minValue", -1f);
                serializer.DataField(ref MaxValue, "maxValue", 2f);
                serializer.DataField(ref MinDuration, "minDuration", -1f);
                serializer.DataField(ref MaxDuration, "maxDuration", 2f);
                ColorsToCycle = serializer.ReadDataField("colorsToCycle", new List<Color>());
            }
        }

        [ViewVariables(VVAccess.ReadOnly)]
        private Dictionary<string, LightBehaviourData> _behaviourData = new Dictionary<string, LightBehaviourData>();

        private int _prevColorIndex = default;
        private int _currentColorIndex = default;
        private float _originalRadius = default;
        private float _originalEnergy = default;
        private Color _originalColor = default;
        private bool _originalEnabled = default;
        private bool _startEnabled = false;
        private IEntity _owner = default;
        private IRobustRandom _random = default;

        private Dictionary<string, float> _timingData = new Dictionary<string, float>();

        private Animation GenerateAnimation(LightBehaviourData data)
        {
            var seconds = (double) data.MaxDuration;

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
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue, 0),
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, (float)seconds * 0.5f),
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue * 0.8f, (float)seconds)
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
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue, 0),
                                    new AnimationTrackProperty.KeyFrame(data.MinValue, (float)seconds * 0.5f),
                                    new AnimationTrackProperty.KeyFrame(data.MaxValue * 0.8f, (float)seconds)
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
                                    new AnimationTrackProperty.KeyFrame(false, MathF.Min(data.MaxValue, (float)seconds))
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

        public override void InitializeEntity(IEntity entity)
        {
            _owner = entity;

            base.InitializeEntity(_owner);

            _random = IoCManager.Resolve<IRobustRandom>();

            var playerComponent = _owner.EnsureComponent<AnimationPlayerComponent>();
            playerComponent.AnimationCompleted += (key => PlayNewAnimation(playerComponent, key));

            CopyLightSettings();

            if (_startEnabled)
            {
                StartLightBehaviour();
            }
        }

        private void CopyLightSettings()
        {
            if (_owner.TryGetComponent<PointLightComponent>(out var pointLight))
            {
                _originalColor = pointLight.Color;
                _originalEnabled = pointLight.Enabled;
                _originalEnergy = pointLight.Energy;
                _originalRadius = pointLight.Radius;
            }
            else
            {
                Logger.Warning($"{_owner.Name} has a {nameof(LightBehaviourVisualizer)} but it has no {nameof(PointLightComponent)}! Check the prototype!");
            }
        }

        /// <summary>
        /// Start animating a light behaviour with the specified ID. If the specified ID is empty, it will start animating all light behaviour entries.
        /// If specified light behaviours are already animating or if the owning entity has no PointLightComponent, calling this does nothing.
        /// </summary>
        public void StartLightBehaviour(string id = "")
        {
            if (!_owner.HasComponent<PointLightComponent>())
            {
                return;
            }

            _currentColorIndex = 1;
            _prevColorIndex = 0;
            var playerComponent = _owner.EnsureComponent<AnimationPlayerComponent>();

            foreach (KeyValuePair<string, LightBehaviourData> pair in _behaviourData)
            {
                if (pair.Value.ID != id && id != string.Empty)
                {
                    continue;
                }

                if ((pair.Value.LightBehaviourType == LightBehaviourType.ColorSequence ||
                    pair.Value.LightBehaviourType == LightBehaviourType.ColorSequenceSmooth) &&
                    pair.Value.ColorsToCycle.Count < 2)
                {
                    Logger.Warning($"{_owner.Name} has a color cycling {nameof(LightBehaviourVisualizer)} with less than 2 colors!");
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
            if (_owner.TryGetComponent<AnimationPlayerComponent>(out var playerComponent))
            {
                var toRemove = new List<string>();

                foreach (KeyValuePair<string, LightBehaviourData> pair in _behaviourData)
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
                        _behaviourData.Remove(key);
                    }
                }

                if (resetToOriginalSettings && _owner.TryGetComponent<PointLightComponent>(out var pointLight))
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

            while (_behaviourData.TryGetValue(key.ToString(), out _))
            {
                key++;
            }

            _behaviourData[nameof(LightBehaviourVisualizer) + key] = data;

            if (playImmediately)
            {
                StartLightBehaviour(data.ID);
            }
        }

        private void PlayNewAnimation(AnimationPlayerComponent playerComponent, string key)
        {
            var animation = GenerateAnimation(_behaviourData[key]);
            playerComponent.Play(animation, key);
        }

        public override void LoadData(YamlMappingNode mapping) // this sucks man. why we gotta do it this way.
        {
            if (mapping.TryGetNode("enabled", out var enabledNode))
            {
                _startEnabled = enabledNode.AsBool();
            }

            var nodeBehaviours = mapping.GetNode("behaviours").AllNodes;
            int key = 0;

            foreach (var node in nodeBehaviours)
            {
                if (node is YamlMappingNode mapNode)
                {
                    LightBehaviourData data = new LightBehaviourData();
                    data.ID = string.Empty;
                    data.MinDuration = -1f;
                    data.MaxDuration = 2f;
                    data.MinValue = -1f;
                    data.MaxValue = 2f;
                    data.LightBehaviourType = LightBehaviourType.Flicker;

                    foreach (KeyValuePair<YamlNode, YamlNode> pair in mapNode.Children)
                    {
                        switch (pair.Key.AsString())
                        {
                            case ("type"):
                                data.LightBehaviourType = pair.Value.AsEnum<LightBehaviourType>();
                                break;
                            case ("maxDuration"):
                                data.MaxDuration = pair.Value.AsFloat();
                                break;
                            case ("minDuration"):
                                data.MinDuration = pair.Value.AsFloat();
                                break;
                            case ("minValue"):
                                data.MinValue = pair.Value.AsFloat();
                                break;
                            case ("maxValue"):
                                data.MaxValue = pair.Value.AsFloat();
                                break;
                            case ("id"):
                                data.ID = pair.Value.AsString();
                                break;
                            case ("colorsToCycle"):
                                var seq = pair.Value as YamlSequenceNode;
                                data.ColorsToCycle = new List<Color>();

                                foreach (var color in seq.Children)
                                {
                                    data.ColorsToCycle.Add(color.AsColor());
                                }

                                break;
                        }           
                    }

                    _behaviourData[nameof(LightBehaviourVisualizer) + key] = data;
                    key++;
                }
            }
        }

        /*public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => _startEnabled, "enabled", false);
            var list = serializer.ReadDataField("behaviours", new List<LightBehaviourData>());
            int key = 0;

            foreach (var behaviour in list)
            {
                _behaviourData[nameof(LightBehaviourVisualizer) + key] = behaviour;
                key++;
            }
        }*/

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }
        }
    }
}
