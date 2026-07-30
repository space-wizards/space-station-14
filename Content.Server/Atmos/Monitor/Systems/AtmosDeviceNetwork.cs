using Content.Server.Atmos.Monitor.Payloads;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Systems;

/// <summary>
///     Generic device network commands useful for atmos devices,
///     as well as some helper commands.
/// </summary>
public sealed partial class AtmosDeviceNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNet = default!;

    public void Register(EntityUid uid, DeviceAddress? address)
    {
        var registerPayload = new AtmosMonitorRegisterDevicePayload();
        _deviceNet.QueuePacket(uid, address, ref registerPayload);
    }

    public void Deregister(EntityUid uid, DeviceAddress? address)
    {
        var deregisterPayload = new AtmosMonitorDeregisterDevicePayload();
        _deviceNet.QueuePacket(uid, address, ref deregisterPayload);
    }

    public void Sync(EntityUid uid, DeviceAddress? address)
    {
        var payload = new AtmosSyncPayload();
        _deviceNet.QueuePacket(uid, address, ref payload);
    }
}
