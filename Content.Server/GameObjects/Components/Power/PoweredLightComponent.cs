using SS14.Server.GameObjects;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    public class PoweredLightComponent : Component
    {
        public override string Name => "PoweredLight";

        public override void Initialize()
        {
            base.Initialize();

            var device = Owner.GetComponent<PowerDeviceComponent>();
            var sprite = Owner.GetComponent<SpriteComponent>();
            var light = Owner.GetComponent<PointLightComponent>();
            device.OnPowerStateChanged += (sender, args) =>
            {
                if (args.Powered)
                {
                    sprite.LayerSetTexture(0, "Objects/wall_light.png");
                    light.State = LightState.On;
                }
                else
                {
                    sprite.LayerSetTexture(0, "Objects/wall_light_off.png");
                    light.State = LightState.Off;
                }
            };
        }
    }
}
