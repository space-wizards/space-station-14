using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.Light.Components
{
    #region LIGHT_BEHAVIOURS
    /// <summary>
    /// Base class for all light behaviours to derive from.
    /// This AnimationTrack derivative does not rely on keyframes since it often needs to have a randomized duration.
    /// </summary>
    [Serializable]
    [ImplicitDataDefinitionForInheritors]
    public abstract class LightBehaviourAnimationTrack : AnimationTrackProperty
    {
        protected IEntityManager _entMan = default!;
        protected IRobustRandom _random = default!;

        [DataField("id")] [ViewVariables] public string ID { get; set; } = string.Empty;

        [DataField("property")]
        [ViewVariables]
        public virtual string Property { get; protected set; } = "Radius";

        [DataField("isLooped")] [ViewVariables] public bool IsLooped { get; set; }

        [DataField("enabled")] [ViewVariables] public bool Enabled { get; set; }

        [DataField("startValue")] [ViewVariables] public float StartValue { get; set; } = 0f;

        [DataField("endValue")] [ViewVariables] public float EndValue { get; set; } = 2f;

        [DataField("minDuration")] [ViewVariables] public float MinDuration { get; set; } = -1f;

        [DataField("maxDuration")] [ViewVariables] public float MaxDuration { get; set; } = 2f;

        [DataField("interpolate")] [ViewVariables] public AnimationInterpolationMode InterpolateMode { get; set; } = AnimationInterpolationMode.Linear;

        [ViewVariables] protected float MaxTime { get; set; }

        private float _maxTime = default;
        private EntityUid _parent = default!;

        public void Initialize(EntityUid parent, IRobustRandom random, IEntityManager entMan)
        {
            _random = random;
            _entMan = entMan;
            _parent = parent;

            if (Enabled && _entMan.TryGetComponent(_parent, out PointLightComponent? light))
            {
                light.Enabled = true;
            }

            OnInitialize();
        }

        public void UpdatePlaybackValues(Animation owner)
        {
            if (_entMan.TryGetComponent(_parent, out PointLightComponent? light))
            {
                light.Enabled = true;
            }

            if (MinDuration > 0)
            {
                MaxTime = (float) _random.NextDouble() * (MaxDuration - MinDuration) + MinDuration;
            }
            else
            {
                MaxTime = MaxDuration;
            }

            owner.Length = TimeSpan.FromSeconds(MaxTime);
        }

        public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
        {
            OnStart();

            return (-1, _maxTime);
        }

        protected void ApplyProperty(object value)
        {
            if (Property == null)
            {
                throw new InvalidOperationException("Property parameter is null! Check the prototype!");
            }

            if (_entMan.TryGetComponent(_parent, out PointLightComponent? light))
            {
                AnimationHelper.SetAnimatableProperty(light, Property, value);
            }
        }

        protected override void ApplyProperty(object context, object value)
        {
            ApplyProperty(value);
        }

        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
    }

    /// <summary>
    /// A light behaviour that alternates between StartValue and EndValue
    /// </summary>
    [UsedImplicitly]
    public sealed class PulseBehaviour : LightBehaviourAnimationTrack
    {
        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
            object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            if (Property == "Enabled") // special case for boolean
            {
                ApplyProperty(interpolateValue < 0.5f);
                return (-1, playingTime);
            }

            if (interpolateValue < 0.5f)
            {
                switch (InterpolateMode)
                {
                    case AnimationInterpolationMode.Linear:
                        ApplyProperty(InterpolateLinear(StartValue, EndValue, interpolateValue * 2f));
                        break;
                    case AnimationInterpolationMode.Cubic:
                        ApplyProperty(InterpolateCubic(EndValue, StartValue, EndValue, StartValue, interpolateValue * 2f));
                        break;
                    default:
                    case AnimationInterpolationMode.Nearest:
                        ApplyProperty(StartValue);
                        break;
                }
            }
            else
            {
                switch (InterpolateMode)
                {
                    case AnimationInterpolationMode.Linear:
                        ApplyProperty(InterpolateLinear(EndValue, StartValue, (interpolateValue - 0.5f) * 2f));
                        break;
                    case AnimationInterpolationMode.Cubic:
                        ApplyProperty(InterpolateCubic(StartValue, EndValue, StartValue, EndValue, (interpolateValue - 0.5f) * 2f));
                        break;
                    default:
                    case AnimationInterpolationMode.Nearest:
                        ApplyProperty(EndValue);
                        break;
                }
            }

            return (-1, playingTime);
        }
    }

    /// <summary>
    /// A light behaviour that interpolates from StartValue to EndValue
    /// </summary>
    [UsedImplicitly]
    public sealed class FadeBehaviour : LightBehaviourAnimationTrack
    {
        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
            object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            if (Property == "Enabled") // special case for boolean
            {
                ApplyProperty(interpolateValue < EndValue);
                return (-1, playingTime);
            }

            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    ApplyProperty(InterpolateLinear(StartValue, EndValue, interpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    ApplyProperty(InterpolateCubic(EndValue, StartValue, EndValue, StartValue, interpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    ApplyProperty(interpolateValue < 0.5f ? StartValue : EndValue);
                    break;
            }

            return (-1, playingTime);
        }
    }

    /// <summary>
    /// A light behaviour that interpolates using random values chosen between StartValue and EndValue.
    /// </summary>
    [UsedImplicitly]
    public sealed class RandomizeBehaviour : LightBehaviourAnimationTrack
    {
        private float _randomValue1;
        private float _randomValue2;
        private float _randomValue3;
        private float _randomValue4;

        public override void OnInitialize()
        {
            _randomValue1 = (float) InterpolateLinear(StartValue, EndValue, (float) _random.NextDouble());
            _randomValue2 = (float) InterpolateLinear(StartValue, EndValue, (float) _random.NextDouble());
            _randomValue3 = (float) InterpolateLinear(StartValue, EndValue, (float) _random.NextDouble());
        }

        public override void OnStart()
        {
            if (Property == "Enabled") // special case for boolean, we randomize it
            {
                ApplyProperty(_random.NextDouble() < 0.5);
                return;
            }

            if (InterpolateMode == AnimationInterpolationMode.Cubic)
            {
                _randomValue1 = _randomValue2;
                _randomValue2 = _randomValue3;
            }

            _randomValue3 = _randomValue4;
            _randomValue4 = (float) InterpolateLinear(StartValue, EndValue, (float) _random.NextDouble());
        }

        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
           object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            if (Property == "Enabled")
            {
                return (-1, playingTime);
            }

            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    ApplyProperty(InterpolateLinear(_randomValue3, _randomValue4, interpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    ApplyProperty(InterpolateCubic(_randomValue1, _randomValue2, _randomValue3, _randomValue4, interpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    ApplyProperty(interpolateValue < 0.5f ? _randomValue3 : _randomValue4);
                    break;
            }

            return (-1, playingTime);
        }
    }

    /// <summary>
    /// A light behaviour that cycles through a list of colors.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ColorCycleBehaviour : LightBehaviourAnimationTrack, ISerializationHooks
    {
        [DataField("property")]
        [ViewVariables]
        public override string Property { get; protected set; } = "Color";

        [DataField("colors")] public List<Color> ColorsToCycle { get; set; } = new();

        private int _colorIndex;

        public override void OnStart()
        {
            _colorIndex++;

            if (_colorIndex > ColorsToCycle.Count - 1)
            {
                _colorIndex = 0;
            }
        }

        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
           object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    ApplyProperty(InterpolateLinear(ColorsToCycle[(_colorIndex - 1) % ColorsToCycle.Count],
                                                                    ColorsToCycle[_colorIndex],
                                                                    interpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    ApplyProperty(InterpolateCubic(ColorsToCycle[_colorIndex],
                                                                    ColorsToCycle[(_colorIndex + 1) % ColorsToCycle.Count],
                                                                    ColorsToCycle[(_colorIndex + 2) % ColorsToCycle.Count],
                                                                    ColorsToCycle[(_colorIndex + 3) % ColorsToCycle.Count],
                                                                    interpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    ApplyProperty(ColorsToCycle[_colorIndex]);
                    break;
            }

            return (-1, playingTime);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            if (ColorsToCycle.Count < 2)
            {
                throw new InvalidOperationException($"{nameof(ColorCycleBehaviour)} has less than 2 colors to cycle");
            }
        }
    }
    #endregion

    /// <summary>
    /// A component which applies a specific behaviour to a PointLightComponent on its owner.
    /// </summary>
    [RegisterComponent]
    public sealed class LightBehaviourComponent : SharedLightBehaviourComponent, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const string KeyPrefix = nameof(LightBehaviourComponent);

        public sealed class AnimationContainer
        {
            public AnimationContainer(int key, Animation animation, LightBehaviourAnimationTrack track)
            {
                Key = key;
                Animation = animation;
                LightBehaviour = track;
            }

            public string FullKey => KeyPrefix + Key;
            public int Key { get; set; }
            public Animation Animation { get; set; }
            public LightBehaviourAnimationTrack LightBehaviour { get; set; }
        }

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("behaviours")]
        public readonly List<LightBehaviourAnimationTrack> Behaviours = new();

        [ViewVariables(VVAccess.ReadOnly)]
        private readonly List<AnimationContainer> _animations = new();

        private float _originalRadius;
        private float _originalEnergy;
        private Angle _originalRotation;
        private Color _originalColor;
        private bool _originalEnabled;

        void ISerializationHooks.AfterDeserialization()
        {
            var key = 0;

            foreach (var behaviour in Behaviours)
            {
                var animation = new Animation()
                {
                    AnimationTracks = {behaviour}
                };

                _animations.Add(new AnimationContainer(key, animation, behaviour));
                key++;
            }
        }

        protected override void Startup()
        {
            base.Startup();

            CopyLightSettings();

            // TODO: Do NOT ensure component here. And use eventbus events instead...
            Owner.EnsureComponent<AnimationPlayerComponent>();

            if (_entMan.TryGetComponent(Owner, out AnimationPlayerComponent? animation))
            {
#pragma warning disable 618
                animation.AnimationCompleted += OnAnimationCompleted;
#pragma warning restore 618
            }

            foreach (var container in _animations)
            {
                container.LightBehaviour.Initialize(Owner, _random, _entMan);
            }

            // we need to initialize all behaviours before starting any
            foreach (var container in _animations)
            {
                if (container.LightBehaviour.Enabled)
                {
                    StartLightBehaviour(container.LightBehaviour.ID);
                }
            }
        }

        private void OnAnimationCompleted(string key)
        {
            var container = _animations.FirstOrDefault(x => x.FullKey == key);

            if (container == null)
            {
                return;
            }

            if (container.LightBehaviour.IsLooped)
            {
                container.LightBehaviour.UpdatePlaybackValues(container.Animation);

                if (_entMan.TryGetComponent(Owner, out AnimationPlayerComponent? animation))
                {
                    animation.Play(container.Animation, container.FullKey);
                }
            }
        }

        /// <summary>
        /// If we disable all the light behaviours we want to be able to revert the light to its original state.
        /// </summary>
        private void CopyLightSettings()
        {
            if (_entMan.TryGetComponent(Owner, out PointLightComponent? light))
            {
                _originalColor = light.Color;
                _originalEnabled = light.Enabled;
                _originalEnergy = light.Energy;
                _originalRadius = light.Radius;
                _originalRotation = light.Rotation;
            }
            else
            {
                Logger.Warning($"{_entMan.GetComponent<MetaDataComponent>(Owner).EntityName} has a {nameof(LightBehaviourComponent)} but it has no {nameof(PointLightComponent)}! Check the prototype!");
            }
        }

        /// <summary>
        /// Start animating a light behaviour with the specified ID. If the specified ID is empty, it will start animating all light behaviour entries.
        /// If specified light behaviours are already animating, calling this does nothing.
        /// Multiple light behaviours can have the same ID.
        /// </summary>
        public void StartLightBehaviour(string id = "")
        {
            if (!_entMan.TryGetComponent(Owner, out AnimationPlayerComponent? animation))
            {
                return;
            }

            foreach (var container in _animations)
            {
                if (container.LightBehaviour.ID == id || id == string.Empty)
                {
                    if (!animation.HasRunningAnimation(KeyPrefix + container.Key))
                    {
                        container.LightBehaviour.UpdatePlaybackValues(container.Animation);
                        animation.Play(container.Animation, KeyPrefix + container.Key);
                    }
                }
            }
        }

        /// <summary>
        /// If any light behaviour with the specified ID is animating, then stop it.
        /// If no ID is specified then all light behaviours will be stopped.
        /// Multiple light behaviours can have the same ID.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="removeBehaviour">Should the behaviour(s) also be removed permanently?</param>
        /// <param name="resetToOriginalSettings">Should the light have its original settings applied?</param>
        public void StopLightBehaviour(string id = "", bool removeBehaviour = false, bool resetToOriginalSettings = false)
        {
            if (!_entMan.TryGetComponent(Owner, out AnimationPlayerComponent? animation))
            {
                return;
            }

            var toRemove = new List<AnimationContainer>();

            foreach (var container in _animations)
            {
                if (container.LightBehaviour.ID == id || id == string.Empty)
                {
                    if (animation.HasRunningAnimation(KeyPrefix + container.Key))
                    {
                        animation.Stop(KeyPrefix + container.Key);
                    }

                    if (removeBehaviour)
                    {
                        toRemove.Add(container);
                    }
                }
            }

            foreach (var container in toRemove)
            {
                _animations.Remove(container);
            }

            if (resetToOriginalSettings && _entMan.TryGetComponent(Owner, out PointLightComponent? light))
            {
                light.Color = _originalColor;
                light.Enabled = _originalEnabled;
                light.Energy = _originalEnergy;
                light.Radius = _originalRadius;
                light.Rotation = _originalRotation;
            }
        }

        /// <summary>
        /// Add a new light behaviour to the component and start it immediately unless otherwise specified.
        /// </summary>
        public void AddNewLightBehaviour(LightBehaviourAnimationTrack behaviour, bool playImmediately = true)
        {
            var key = 0;

            while (_animations.Any(x => x.Key == key))
            {
                key++;
            }

            var animation = new Animation()
            {
                AnimationTracks = {behaviour}
            };

            behaviour.Initialize(Owner, _random, _entMan);

            var container = new AnimationContainer(key, animation, behaviour);
            _animations.Add(container);

            if (playImmediately)
            {
                StartLightBehaviour(behaviour.ID);
            }
        }
    }
}
