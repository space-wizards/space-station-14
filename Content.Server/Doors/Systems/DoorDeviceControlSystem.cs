using Content.Server.Doors.Components;
using Content.Server.DeviceNetwork.Systems;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorDeviceControlComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<DoorDeviceControlComponent, DoorStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<DoorDeviceControlComponent, DoorBoltsChangedEvent>(OnBoltsChanged);
    }

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
                if (TryComp<DoorComponent>(ent, out var toOpen) && toOpen.State == DoorState.Closed)
                    _doors.TryOpen(ent, toOpen);
                break;

            case DoorNetworkCommands.Close:
                if (TryComp<DoorComponent>(ent, out var toClose) && toClose.State == DoorState.Open)
                    _doors.TryClose(ent, toClose);
                break;

            case DoorNetworkCommands.Bolt:
                if (TryComp<DoorBoltComponent>(ent, out var toBolt))
                    _doors.SetBoltsDown((ent, toBolt), true);
                break;

            case DoorNetworkCommands.Unbolt:
                if (TryComp<DoorBoltComponent>(ent, out var toUnbolt))
                    _doors.SetBoltsDown((ent, toUnbolt), false);
                break;
        }
    }

    private void OnStateChanged(Entity<DoorDeviceControlComponent> ent, ref DoorStateChangedEvent args)
    {
        PushStatus(ent);
    }

    private void OnBoltsChanged(Entity<DoorDeviceControlComponent> ent, ref DoorBoltsChangedEvent args)
    {
        PushStatus(ent);
    }

    /// <summary>
    ///     Tells whoever listed us that we moved
    /// </summary>
    private void PushStatus(EntityUid uid)
    {
        if (!TryComp<DeviceNetworkComponent>(uid, out var net) || net.DeviceLists.Count == 0)
            return;

        var payload = Status(uid);

        foreach (var list in net.DeviceLists)
        {
            if (TryComp<DeviceNetworkComponent>(list, out var listener) && listener.ReceiveFrequency != null)
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
        var boltable = TryComp<DoorBoltComponent>(uid, out var bolts);

        return new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DoorNetworkCommands.Status,
            [DoorNetworkCommands.StatusOpen] = !TryComp<DoorComponent>(uid, out var door) || door.State != DoorState.Closed,
            [DoorNetworkCommands.StatusBolted] = boltable && bolts!.BoltsDown,
            [DoorNetworkCommands.StatusBoltable] = boltable,
        };
    }
}
