using Content.Server.Atmos.Monitor.Payloads;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Systems;

/// <summary>
///     Generic device network commands useful for atmos devices,
///     as well as some helper commands.
/// </summary>
public sealed partial class AtmosDeviceNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNet = default!;

    public void Register(EntityUid uid, string? address)
    {
        var registerPayload = new AtmosMonitorRegisterDevicePayload();
        _deviceNet.QueuePacket(uid, address, registerPayload);
    }

    public void Deregister(EntityUid uid, string? address)
    {
        var deregisterPayload = new AtmosMonitorDeregisterDevicePayload();
        _deviceNet.QueuePacket(uid, address, deregisterPayload);
    }

    public void Sync(EntityUid uid, string? address)
    {
        var payload = new AtmosSyncPayload();
        _deviceNet.QueuePacket(uid, address, payload);
    }
}
