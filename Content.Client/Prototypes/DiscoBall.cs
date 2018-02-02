using SS14.Client.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;

namespace Content.Client.Prototypes
{
    // Instantiated through reflection by the prototype system.
    public class DiscoBall : Entity
    {
        private PointLightComponent _lightComponent;
        private float _hue;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            _lightComponent = GetComponent<PointLightComponent>();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();
            _lightComponent = null;
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            _hue += frameTime / 10;
            if (_hue > 1)
            {
                _hue -= 1;
            }

            _lightComponent.Color = Color4.FromHsl(new Vector4(_hue, 1, 0.5f, 0.5f));
        }
    }
}
