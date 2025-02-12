using Content.Shared.Light.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Client.Light.Components
{
    #region LIGHT_BEHAVIOURS
    /// <summary>
    /// Base class for all light behaviours to derive from.
    /// This AnimationTrack derivative does not rely on keyframes since it often needs to have a randomized duration.
    /// </summary>
    [Serializable]
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class LightBehaviourAnimationTrack : AnimationTrackProperty
    {
        protected IEntityManager _entMan = default!;
        protected IRobustRandom _random = default!;

        [DataField("id")] public string ID { get; set; } = string.Empty;

        [DataField("property")]
        public virtual string Property { get; protected set; } = nameof(PointLightComponent.AnimatedRadius);

        [DataField("isLooped")] public bool IsLooped { get; set; }

        [DataField("enabled")] public bool Enabled { get; set; }

        [DataField("startValue")] public float StartValue { get; set; } = 0f;

        [DataField("endValue")] public float EndValue { get; set; } = 2f;

        [DataField("minDuration")] public float MinDuration { get; set; } = -1f;

        [DataField("maxDuration")] public float MaxDuration { get; set; } = 2f;

        [DataField("interpolate")] public AnimationInterpolationMode InterpolateMode { get; set; } = AnimationInterpolationMode.Linear;

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
                _entMan.System<PointLightSystem>().SetEnabled(_parent, true, light);
            }

            OnInitialize();
        }

        public void UpdatePlaybackValues(Animation owner)
        {
            if (_entMan.TryGetComponent(_parent, out PointLightComponent? light))
            {
                _entMan.System<PointLightSystem>().SetEnabled(_parent, true, light);
            }

            if (MinDuration > 0)
            {
                MaxTime = (float)_random.NextDouble() * (MaxDuration - MinDuration) + MinDuration;
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
    public sealed partial class PulseBehaviour : LightBehaviourAnimationTrack
    {
        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
            object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            if (Property == nameof(PointLightComponent.AnimatedEnable)) // special case for boolean
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
    public sealed partial class FadeBehaviour : LightBehaviourAnimationTrack
    {
        /// <summary>
        /// Automatically reverse the animation when EndValue is reached. In this particular case, MaxTime specifies the
        /// time of the full animation, including the reverse interpolation.
        /// </summary>
        [DataField("reverseWhenFinished")]
        public bool ReverseWhenFinished { get; set; }

        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
            object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            if (Property == nameof(PointLightComponent.AnimatedEnable)) // special case for boolean
            {
                ApplyProperty(interpolateValue < EndValue);
                return (-1, playingTime);
            }

            // From 0 to MaxTime/2, we go from StartValue to EndValue. From MaxTime/2 to MaxTime, we reverse this interpolation.
            if (ReverseWhenFinished)
            {
                if (interpolateValue < 0.5f)
                {
                    ApplyInterpolation(StartValue, EndValue, interpolateValue * 2);
                }
                else
                {
                    ApplyInterpolation(EndValue, StartValue, (interpolateValue - 0.5f) * 2);
                }
            }
            else
            {
                ApplyInterpolation(StartValue, EndValue, interpolateValue);
            }

            return (-1, playingTime);
        }

        private void ApplyInterpolation(float start, float end, float interpolateValue)
        {
            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    ApplyProperty(InterpolateLinear(start, end, interpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    ApplyProperty(InterpolateCubic(end, start, end, start, interpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    ApplyProperty(interpolateValue < 0.5f ? start : end);
                    break;
            }
        }
    }

    /// <summary>
    /// A light behaviour that interpolates using random values chosen between StartValue and EndValue.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class RandomizeBehaviour : LightBehaviourAnimationTrack
    {
        private float _randomValue1;
        private float _randomValue2;
        private float _randomValue3;
        private float _randomValue4;

        public override void OnInitialize()
        {
            _randomValue1 = (float)InterpolateLinear(StartValue, EndValue, (float)_random.NextDouble());
            _randomValue2 = (float)InterpolateLinear(StartValue, EndValue, (float)_random.NextDouble());
            _randomValue3 = (float)InterpolateLinear(StartValue, EndValue, (float)_random.NextDouble());
        }

        public override void OnStart()
        {
            if (Property == nameof(PointLightComponent.AnimatedEnable)) // special case for boolean, we randomize it
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
            _randomValue4 = (float)InterpolateLinear(StartValue, EndValue, (float) _random.NextDouble());
        }

        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
           object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            var playingTime = prevPlayingTime + frameTime;
            var interpolateValue = playingTime / MaxTime;

            if (Property == nameof(PointLightComponent.AnimatedEnable))
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
    public sealed partial class ColorCycleBehaviour : LightBehaviourAnimationTrack, ISerializationHooks
    {
        [DataField("property")]
        public override string Property { get; protected set; } = nameof(PointLightComponent.Color);

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
    public sealed partial class LightBehaviourComponent : SharedLightBehaviourComponent, ISerializationHooks
    {
        public const string KeyPrefix = nameof(LightBehaviourComponent);

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
        public List<LightBehaviourAnimationTrack> Behaviours = new();

        [ViewVariables(VVAccess.ReadOnly)]
        public readonly List<AnimationContainer> Animations = new();

        [ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<string, object> OriginalPropertyValues = new();

        void ISerializationHooks.AfterDeserialization()
        {
            var key = 0;

            foreach (var behaviour in Behaviours)
            {
                var animation = new Animation()
                {
                    AnimationTracks = { behaviour }
                };

                Animations.Add(new AnimationContainer(key, animation, behaviour));
                key++;
            }
        }
    }
}
