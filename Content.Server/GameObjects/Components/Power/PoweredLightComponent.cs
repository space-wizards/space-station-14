using System;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;

namespace Content.Server.GameObjects.Components.Power
{
    public class PoweredLightComponent : Component
    {
        public override string Name => "PoweredLight";

        private static readonly TimeSpan _thunkDelay = TimeSpan.FromSeconds(2);

        private TimeSpan _lastThunk;

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
                    var time = IoCManager.Resolve<IGameTiming>().CurTime;
                    if (time > _lastThunk + _thunkDelay)
                    {
                        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>()
                            .Play("/Audio/machines/light_tube_on.ogg", Owner, AudioParams.Default.WithVolume(-10f));
                    }
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
