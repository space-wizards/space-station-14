using Content.Server.DeviceNetwork.Systems;
using Content.Server.Doors.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;

namespace Content.Server.Doors.Systems;

/// <summary>
///     Serves the DoorNetworkCommands protocol so an airlock controller can
///     use a door over the device network and ask what it's doing.
///     Stateless: replies go to whoever asked, pushes go to whoever listed us.
/// </summary>
public sealed partial class DoorDeviceControlSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private DoorSystem _doors = default!;

    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<DoorBoltComponent> _boltQuery;
    private EntityQuery<DeviceNetworkComponent> _netQuery;

    public override void Initialize()
    {
        base.Initialize();

        _doorQuery = GetEntityQuery<DoorComponent>();
        _boltQuery = GetEntityQuery<DoorBoltComponent>();
        _netQuery = GetEntityQuery<DeviceNetworkComponent>();
    }

    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<DoorDeviceControlComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        // Device-link traffic doesn't have commands so we ignore those
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        switch (command)
        {
            case DoorNetworkCommands.Sync:
                Reply(ent, args);
                break;

            case DoorNetworkCommands.Open:
                if (_doorQuery.TryComp(ent, out var toOpen) && toOpen.State == DoorState.Closed)
                    _doors.TryOpen(ent, toOpen);
                break;

            case DoorNetworkCommands.Close:
                if (_doorQuery.TryComp(ent, out var toClose) && toClose.State == DoorState.Open)
                    _doors.TryClose(ent, toClose);
                break;

            case DoorNetworkCommands.Bolt:
                if (_boltQuery.TryComp(ent, out var toBolt))
                    _doors.SetBoltsDown((ent, toBolt), true);
                break;

            case DoorNetworkCommands.Unbolt:
                if (_boltQuery.TryComp(ent, out var toUnbolt))
                    _doors.SetBoltsDown((ent, toUnbolt), false);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<DoorDeviceControlComponent> ent, ref DoorStateChangedEvent args)
    {
        PushStatus(ent);
    }

    [SubscribeLocalEvent]
    private void OnBoltsChanged(Entity<DoorDeviceControlComponent> ent, ref DoorBoltsChangedEvent args)
    {
        PushStatus(ent);
    }

    /// <summary>
    ///     Send a status update when our status changes, to device lists which have this device on them.
    /// </summary>
    private void PushStatus(EntityUid uid)
    {
        if (!_netQuery.TryComp(uid, out var net) || net.DeviceLists.Count == 0)
            return;

        var payload = Status(uid);

        foreach (var list in net.DeviceLists)
        {
            if (_netQuery.TryComp(list, out var listener) && listener.ReceiveFrequency != null)
                _deviceNetwork.QueuePacket(uid, listener.Address, payload, listener.ReceiveFrequency.Value, listener.DeviceNetId);
        }
    }

    private void Reply(EntityUid uid, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DoorNetworkCommands.ReplyNetId, out int netId)
            || !args.Data.TryGetValue(DoorNetworkCommands.ReplyFrequency, out uint frequency))
        {
            return;
        }

        _deviceNetwork.QueuePacket(uid, args.SenderAddress, Status(uid), frequency, netId);
    }

    private NetworkPayload Status(EntityUid uid)
    {
        var boltable = _boltQuery.TryComp(uid, out var bolts);

        return new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DoorNetworkCommands.Status,
            [DoorNetworkCommands.StatusOpen] = !_doorQuery.TryComp(uid, out var door) || door.State != DoorState.Closed,
            [DoorNetworkCommands.StatusBolted] = boltable && bolts!.BoltsDown,
            [DoorNetworkCommands.StatusBoltable] = boltable,
        };
    }
}
