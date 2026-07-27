using Content.Client.Buffers;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Client.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    public override void Initialize()
    {
        base.Initialize();
        DeviceArrayPool = new ClientBaseContentArrayPool<Device>(256, 8);
        EntityArrayPool = new ClientBaseContentArrayPool<EntityUid?>(256, 8);
    }
}
