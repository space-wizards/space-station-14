using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.DeviceNetwork
{
    [UsedImplicitly]
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
