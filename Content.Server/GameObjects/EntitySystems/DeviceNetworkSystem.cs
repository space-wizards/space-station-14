using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    internal sealed class DeviceNetworkSystem : EntitySystem
    {
        [Dependency] private readonly IDeviceNetwork _network = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _network.Update();
        }
    }
}
