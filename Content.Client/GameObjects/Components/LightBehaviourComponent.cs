using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Shared.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Client.GameObjects.Components
{
    #region LIGHT_BEHAVIOURS
    /// <summary>
    /// Base class for all light behaviours to derive from.
    /// </summary>
    [Serializable]
    public abstract class LightBehaviour: IExposeData
    {
        [ViewVariables] public string ID { get; set; }
        [ViewVariables] public string Property { get; protected set; }
        [ViewVariables] public bool IsLooped { get; set; }
        [ViewVariables] public bool Enabled { get; set; }
        [ViewVariables] public float StartValue { get; set; }
        [ViewVariables] public float EndValue { get; set; }
        [ViewVariables] public float MinDuration { get; set; }
        [ViewVariables] public float MaxDuration { get; set; }
        [ViewVariables] public AnimationInterpolationMode InterpolateMode { get; set; }

        [ViewVariables]
        protected float InterpolateValue => _curTime / _maxTime;
        protected PointLightComponent Light = default;
        protected IRobustRandom RobustRandom = default;

        private float _curTime = default;
        private float _maxTime = default;
        
        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.IsLooped, "isLooped", false);
            serializer.DataField(this, x => x.Enabled, "enabled", false);
            serializer.DataField(this, x => x.StartValue, "startValue", 0f);
            serializer.DataField(this, x => x.EndValue, "endValue", 2f);
            serializer.DataField(this, x => x.MinDuration, "minDuration", -1f);
            serializer.DataField(this, x => x.MaxDuration, "maxDuration", 2f);
            serializer.DataField(this, x => x.Property, "property", "Radius");
            serializer.DataField(this, x => x.InterpolateMode, "interpolate", AnimationInterpolationMode.Linear);
        }

        public void Initialize(PointLightComponent light)
        {
            Light = light;
            RobustRandom = IoCManager.Resolve<IRobustRandom>();
            _curTime = 0;
            _maxTime = (float) RobustRandom.NextDouble() * (MaxDuration - MinDuration) + MinDuration;

            if (Enabled)
            {
                Light.Enabled = true;
            }

            OnInitialize();
        }

        public void StartBehaviour()
        {
            Light.Enabled = true;
            Enabled = true;
            _curTime = 0;

            if (MinDuration > 0)
            {
                _maxTime = (float) RobustRandom.NextDouble() * (MaxDuration - MinDuration) + MinDuration;
            }
            else
            {
                _maxTime = MaxDuration;
            }
            
            OnStart();
        }

        public void Update(float frameTime)
        {
            _curTime += frameTime;

            if (_curTime > _maxTime)
            {
                if (!IsLooped)
                {
                    Enabled = false;
                    return;
                }

                StartBehaviour();
            }

            OnUpdate(frameTime);
        }

        protected static object? GetProperty(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName);

            if (property == null)
            {
                throw new ArgumentException($"{nameof(LightBehaviourComponent)} attempting to modify nonexistant property with name '{propertyName}'.");
            }

            return property.GetValue(target);
        }

        protected static void SetProperty(object target, string propertyName, object value)
        {
            var property = target.GetType().GetProperty(propertyName);

            if (property == null)
            {
                throw new ArgumentException($"{nameof(LightBehaviourComponent)} attempting to modify nonexistant property with name '{propertyName}'.");
            }

            // todo: Fix this in the engine. Setting the radius of a light to 0 causes exceptions.
            // The user shouldn't be doing that anyways but the safety check should still be there.
            if (propertyName == "Radius" && (float)value <= 0f)
            {
                value = 0.05f;
            }

            property.SetValue(target, value);
        }

        protected static object InterpolateLinear(object a, object b, float t)
        {
            switch (a)
            {
                case Vector2 vector2:
                    return Vector2.Lerp(vector2, (Vector2) b, t);
                case Vector3 vector3:
                    return Vector3.Lerp(vector3, (Vector3) b, t);
                case Vector4 vector4:
                    return Vector4.Lerp(vector4, (Vector4) b, t);
                case float f:
                    return MathHelper.Lerp(f, (float) b, t);
                case double d:
                    return MathHelper.Lerp(d, (double) b, t);
                case Angle angle:
                    return (Angle) MathHelper.Lerp(angle, (Angle) b, t);
                case Color color:
                    return Color.InterpolateBetween(color, (Color) b, t);
                case int i:
                    return (int) MathHelper.Lerp((double) i, (int) b, t);
                default:
                    // Fall back to "previous" interpolation, treating this as a discrete value.
                    return a;
            }
        }

        protected static object InterpolateCubic(object preA, object a, object b, object postB, float t)
        {
            switch (a)
            {
                case Vector2 vector2:
                    return Vector2.InterpolateCubic((Vector2) preA, vector2, (Vector2) b, (Vector2) postB, t);
                case Vector3 vector3:
                    return Vector3.InterpolateCubic((Vector3) preA, vector3, (Vector3) b, (Vector3) postB, t);
                case Vector4 vector4:
                    return Vector4.InterpolateCubic((Vector4) preA, vector4, (Vector4) b, (Vector4) postB, t);
                case float f:
                    return MathHelper.InterpolateCubic((float) preA, f, (float) b, (float) postB, t);
                case double d:
                    return MathHelper.InterpolateCubic((double) preA, d, (double) b, (double) postB, t);
                case int i:
                    return (int) MathHelper.InterpolateCubic((int) preA, (double) i, (int) b, (int) postB, t);
                default:
                    // Fall back to "previous" interpolation, treating this as a discrete value.
                    return a;
            }
        }

        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
        public virtual void OnUpdate(float frameTime) { }
    }

    /// <summary>
    /// A light behaviour that constantly pulses between StartValue and EndValue
    /// </summary>
    public class PulseBehaviour: LightBehaviour
    {
        public override void OnUpdate(float frameTime)
        {
            if (Property == "Enabled") // special case for boolean, we use MaxValue to determine when to enable/disable
            {
                SetProperty(Light, Property, InterpolateValue < EndValue ? true : false);
                return;
            }

            if (InterpolateValue < 0.5f)
            {
                switch(InterpolateMode)
                {
                    case AnimationInterpolationMode.Linear:
                        SetProperty(Light, Property, InterpolateLinear(StartValue, EndValue, InterpolateValue * 2f));
                        break;
                    case AnimationInterpolationMode.Cubic:
                        SetProperty(Light, Property, InterpolateCubic(EndValue, StartValue, EndValue, StartValue, InterpolateValue * 2f));
                        break;
                    default:
                    case AnimationInterpolationMode.Nearest:
                        SetProperty(Light, Property, StartValue);
                        break;
                }
            }
            else
            {
                switch (InterpolateMode)
                {
                    case AnimationInterpolationMode.Linear:
                        SetProperty(Light, Property, InterpolateLinear(EndValue, StartValue, (InterpolateValue - 0.5f) * 2f));
                        break;
                    case AnimationInterpolationMode.Cubic:
                        SetProperty(Light, Property, InterpolateCubic(StartValue, EndValue, StartValue, EndValue, (InterpolateValue - 0.5f) * 2f));
                        break;
                    default:
                    case AnimationInterpolationMode.Nearest:
                        SetProperty(Light, Property, EndValue);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// A light behaviour that interpolates from StartValue to EndValue
    /// </summary>
    public class FadeBehaviour : LightBehaviour
    {
        public override void OnUpdate(float frameTime)
        {
            if (Property == "Enabled") // special case for boolean, we use MaxValue to determine when to enable/disable
            {
                SetProperty(Light, Property, InterpolateValue < 0.5f ? true : false);
                return;
            }

            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    SetProperty(Light, Property, InterpolateLinear(StartValue, EndValue, InterpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    SetProperty(Light, Property, InterpolateCubic(EndValue, StartValue, EndValue, StartValue, InterpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    SetProperty(Light, Property, InterpolateValue < 0.5f ? StartValue : EndValue);
                    break;
            }
        }
    }

    /// <summary>
    /// A light behaviour that interpolates random values chosen between StartValue and EndValue.
    /// </summary>
    public class RandomizeBehaviour : LightBehaviour
    {
        private object? _randomValue1 = default;
        private object? _randomValue2 = default;
        private object? _randomValue3 = default;
        private object? _randomValue4 = default;

        public override void OnInitialize()
        {
            _randomValue2 = InterpolateLinear(StartValue, EndValue, (float) RobustRandom.NextDouble());
            _randomValue3 = InterpolateLinear(StartValue, EndValue, (float) RobustRandom.NextDouble());
            _randomValue4 = InterpolateLinear(StartValue, EndValue, (float) RobustRandom.NextDouble());
        }

        public override void OnStart()
        {
            if (Property == "Enabled") // special case for boolean, we randomize it
            {
                SetProperty(Light, Property, RobustRandom.NextDouble() < 0.5 ? true : false);
                return;
            }

            if (InterpolateMode == AnimationInterpolationMode.Cubic)
            {
                _randomValue1 = _randomValue2;
                _randomValue2 = _randomValue3;
            }

            _randomValue3 = _randomValue4;
            _randomValue4 = InterpolateLinear(StartValue, EndValue, (float) RobustRandom.NextDouble());
        }

        public override void OnUpdate(float frameTime)
        {
            if (Property == "Enabled")
            {
                return;
            }

            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    SetProperty(Light, Property, InterpolateLinear(_randomValue3, _randomValue4, InterpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    SetProperty(Light, Property, InterpolateCubic(_randomValue1, _randomValue2, _randomValue3, _randomValue4, InterpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    SetProperty(Light, Property, InterpolateValue < 0.5f ? _randomValue3 : _randomValue4);
                    break;
            }
        }
    }

    /// <summary>
    /// A light behaviour that cycles through a list of colors.
    /// </summary>
    public class ColorCycleBehaviour : LightBehaviour
    {
        public List<Color> ColorsToCycle { get; set; }

        private int _colorIndex = 0;

        public override void OnStart()
        {
            _colorIndex++;

            if (_colorIndex > ColorsToCycle.Count - 1)
            {
                _colorIndex = 0;
            }
        }

        public override void OnUpdate(float frameTime)
        {
            switch (InterpolateMode)
            {
                case AnimationInterpolationMode.Linear:
                    SetProperty(Light, Property, InterpolateLinear(ColorsToCycle[(_colorIndex - 1) % ColorsToCycle.Count],
                                                                    ColorsToCycle[_colorIndex],
                                                                    InterpolateValue));
                    break;
                case AnimationInterpolationMode.Cubic:
                    SetProperty(Light, Property, InterpolateCubic(ColorsToCycle[_colorIndex],
                                                                    ColorsToCycle[(_colorIndex + 1) % ColorsToCycle.Count],
                                                                    ColorsToCycle[(_colorIndex + 2) % ColorsToCycle.Count],
                                                                    ColorsToCycle[(_colorIndex + 3) % ColorsToCycle.Count],
                                                                    InterpolateValue));
                    break;
                default:
                case AnimationInterpolationMode.Nearest:
                    SetProperty(Light, Property, ColorsToCycle[_colorIndex]);
                    break;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.IsLooped, "isLooped", false);
            serializer.DataField(this, x => x.Enabled, "enabled", false);
            serializer.DataField(this, x => x.StartValue, "minValue", 0f);
            serializer.DataField(this, x => x.EndValue, "maxValue", 2f);
            serializer.DataField(this, x => x.MinDuration, "minDuration", -1f);
            serializer.DataField(this, x => x.MaxDuration, "maxDuration", 2f);
            serializer.DataField(this, x => x.InterpolateMode, "interpolate", AnimationInterpolationMode.Linear);
            ColorsToCycle = serializer.ReadDataField("colors", new List<Color>());
            Property = "Color";

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
    public class LightBehaviourComponent : SharedLightBehaviourComponent 
    {
        [ViewVariables(VVAccess.ReadOnly)]
        protected List<LightBehaviour> Behaviours = new List<LightBehaviour>();

        private float _originalRadius = default;
        private float _originalEnergy = default;
        private Angle _originalRotation = default;
        private Color _originalColor = default;
        private bool _originalEnabled = default;
        private PointLightComponent _lightComponent = default;

        protected override void Startup()
        {
            base.Startup();

            CopyLightSettings();
        }

        /// <summary>
        /// If we disable all the light behaviours we want to be able to revert the light to its original state.
        /// </summary>
        private void CopyLightSettings()
        {
            if (Owner.TryGetComponent(out _lightComponent))
            {
                _originalColor = _lightComponent.Color;
                _originalEnabled = _lightComponent.Enabled;
                _originalEnergy = _lightComponent.Energy;
                _originalRadius = _lightComponent.Radius;
                _originalRotation = _lightComponent.Rotation;

                foreach (LightBehaviour behaviour in Behaviours)
                {
                    behaviour.Initialize(_lightComponent);
                }
            }
            else
            {
                Logger.Warning($"{Owner.Name} has a {nameof(LightBehaviourComponent)} but it has no {nameof(PointLightComponent)}! Check the prototype!");
            }
        }

        public void Update(float frameTime)
        {
            foreach (var behaviour in Behaviours)
            {
                if (behaviour.Enabled)
                {
                    behaviour.Update(frameTime);
                }
            }
        }

        /// <summary>
        /// Start animating a light behaviour with the specified ID. If the specified ID is empty, it will start animating all light behaviour entries.
        /// If specified light behaviours are already animating, calling this does nothing.
        /// Multiple light behaviours can have the same ID.
        /// </summary>
        public void StartLightBehaviour(string id = "")
        {
            foreach (var behaviour in Behaviours)
            {
                if (behaviour.ID != id && id != string.Empty)
                {
                    continue;
                }

                behaviour.StartBehaviour();
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
            var toRemove = new List<LightBehaviour>();

            foreach (var behaviour in Behaviours)
            {
                if (behaviour.ID == id || id == string.Empty)
                {
                    behaviour.Enabled = false;

                    if (removeBehaviour)
                    {
                        toRemove.Add(behaviour);
                    } 
                }
            }

            foreach (var behaviour in toRemove)
            {
                Behaviours.Remove(behaviour);
            }

            if (resetToOriginalSettings)
            {
                _lightComponent.Color = _originalColor;
                _lightComponent.Enabled = _originalEnabled;
                _lightComponent.Energy = _originalEnergy;
                _lightComponent.Radius = _originalRadius;
                _lightComponent.Rotation = _originalRotation;
            }
        }

        /// <summary>
        /// Add a new light behaviour to the component and start it immediately unless otherwise specified.
        /// </summary>
        public void AddNewLightBehaviour(LightBehaviour behaviour, bool playImmediately = true)
        {
            Behaviours.Add(behaviour);
            behaviour.Initialize(_lightComponent);

            if (playImmediately)
            {
                StartLightBehaviour(behaviour.ID);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            Behaviours = serializer.ReadDataField("behaviours", new List<LightBehaviour>());
        }
    }
}
