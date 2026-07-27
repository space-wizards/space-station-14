using Content.Shared.DeviceNetwork;
using Content.Server.Buffers;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.DeviceNetwork.Systems;

/// <inheritdoc/>
public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    public override void Initialize()
    {
        base.Initialize();
        DeviceArrayPool = new ServerArrayPool<Device>();
        EntityArrayPool = new ServerArrayPool<EntityUid?>();
    }
}
