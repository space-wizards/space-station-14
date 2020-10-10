using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class DeviceNetworkSystem : EntitySystem
    {
        private IDeviceNetwork _network;

        public override void Initialize()
        {
            base.Initialize();

            _network = IoCManager.Resolve<IDeviceNetwork>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_network == null)
                return;
            //(ノ°Д°）ノ︵ ┻━┻
            _network.Update();
        }
    }
}
