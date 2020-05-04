using System;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components
{
    /// <summary>
    /// A PointLight intended to be removed after a short period of time
    /// </summary>
    public class BriefLightComponent : Component
    {
        public override string Name => "BriefLight";
        public TimeSpan StartTime { get; set; }
        public double Duration { get; set; }
        public TimeSpan EndTime => TimeSpan.FromSeconds(StartTime.TotalSeconds + Duration);

        private Color _color = Color.White;

        public int LightRadius
        {
            get => _lightRadius;
            set
            {
                _lightRadius = value;
                _pointLightComponent.Radius = value;
            }
        }
        private int _lightRadius;
        private PointLightComponent _pointLightComponent;

        public override void OnAdd()
        {
            base.OnAdd();
            if (Owner.HasComponent<PointLightComponent>())
            {
                throw new InvalidOperationException();
            }

            _pointLightComponent = Owner.AddComponent<PointLightComponent>();
            _pointLightComponent.Enabled = true;
            _pointLightComponent.Radius = _lightRadius;
            _pointLightComponent.Color = _color;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (!Owner.HasComponent<PointLightComponent>())
            {
                throw new InvalidOperationException();
            }

            Owner.RemoveComponent<PointLightComponent>();
        }

        public void SetAlpha(double value)
        {
            _color = _color.WithAlpha((float) value);
        }
    }
}
