using SS14.Client.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;

namespace Content.Client.Prototypes
{
    public class DiscoBall : Entity
    {
        private PointLightComponent LightComponent;
        private float Hue;

        public override void Initialize()
        {
            base.Initialize();
            LightComponent = GetComponent<PointLightComponent>();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            LightComponent = null;
        }

        public override void Update(float frameTime)
        {
            Hue += frameTime / 10;
            if (Hue > 1)
            {
                Hue -= 1;
            }

            LightComponent.Color = Color4.FromHsl(new Vector4(Hue, 1, 0.5f, 0.5f));
        }
    }
}
