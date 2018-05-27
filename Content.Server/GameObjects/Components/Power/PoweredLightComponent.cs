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
                    sprite.LayerSetState(0, "on");
                    light.State = LightState.On;
                }
                else
                {
                    sprite.LayerSetState(0, "off");
                    light.State = LightState.Off;
                }
            };
        }
    }
}
