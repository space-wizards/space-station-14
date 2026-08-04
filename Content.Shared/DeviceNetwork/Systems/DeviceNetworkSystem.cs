using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other
///     while adhering to restrictions like range or being connected to the same power network.
/// </summary>
public sealed partial class DeviceNetworkSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private MetaDataSystem _meta = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceQuery = default!;

    private Device[] _deviceCache = [];

    [SubscribeLocalEvent]
    private void OnExamine(Entity<DeviceNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminableAddress)
            args.PushText(Loc.GetString("device-address-examine-message", ("address", DeviceLocalizationHelpers.GetAddressFromId(ent.Comp))));
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<DeviceNetworkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.AutoConnect)
            ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Automatically attempt to connect some devices when a map starts.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.Data.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && _protoMan.TryIndex(device.ReceiveFrequencyId, out var receive))
        {
            device.Data.ReceiveFrequency = receive.Frequency;
        }

        if (device.Data.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && _protoMan.TryIndex(device.TransmitFrequencyId, out var xmit))
        {
            device.Data.TransmitFrequency = xmit.Frequency;
        }

        if (ent.Comp.AutoConnect)
            ConnectDevice(ent.AsNullable());

        DirtyField(ent.AsNullable(), nameof(DeviceNetworkComponent.Data));
    }

    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<DeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        if (TryGetNetwork(ent.AsNullable(), ent.Comp.DeviceNetId, out var network))
            RemoveFromNetwork(ent.AsNullable(), network.Value);
    }

    [SubscribeLocalEvent]
    private void OnManagerInit(Entity<DeviceNetworkManagerComponent> ent, ref ComponentInit args)
    {
        _pvs.AddGlobalOverride(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBeforeSave(BeforeSerializationEvent ev)
    {
        // Device network managers are reconstructable on map-init,
        // so saving them will just bloat the save file.
        var query = AllEntityQuery<DeviceNetworkManagerComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            Del(uid);
        }
    }

    private void SendPacket<T>(ref DeviceNetworkPacketEvent<T> packet) where T : INetworkPayload
    {
        if (!TryEnsureNetwork(packet.Sender.AsNullable(), packet.NetId, out var network))
            return;

        if (packet.Address == null)
        {
            // Broadcast to all listening devices
            if (!network.Value.Comp.ListeningDevices.TryGetValue(packet.Frequency, out var devices)
                || !CheckRecipientsList(network.Value, packet, ref devices))
                return;

            Extensions.EnsureLength(ref _deviceCache, devices.Count);
            devices.CopyTo(_deviceCache);
            SendToConnections(_deviceCache.AsSpan(0, devices.Count), packet);
        }
        else
        {
            var totalDevices = 0;
            var hasTargetedDevice = false;
            if (network.Value.Comp.ReceiveAllDevices.TryGetValue(packet.Frequency, out var devices))
            {
                totalDevices += devices.Count;
            }

            if (!TryGetDevice(network.Value, packet.Address.Value, out var device))
                return;

            if (!device.Value.DeviceData.ReceiveAll &&
                device.Value.DeviceData.ReceiveFrequency == packet.Frequency)
            {
                totalDevices += 1;
                hasTargetedDevice = true;
            }

            Extensions.EnsureLength(ref _deviceCache, totalDevices);
            devices?.CopyTo(_deviceCache);

            if (hasTargetedDevice)
            {
                _deviceCache[totalDevices - 1] = device.Value;
            }
            SendToConnections(_deviceCache.AsSpan(0, totalDevices), packet);
        }
    }

    /// <summary>
    /// Sends the <see cref="BeforeBroadcastAttemptEvent"/> to the sending entity if the packets SendBeforeBroadcastAttemptEvent field is set to true.
    /// The recipients is set to the modified recipient list.
    /// </summary>
    /// <returns>false if the broadcast was canceled</returns>
    private bool CheckRecipientsList<T>(Entity<DeviceNetworkManagerComponent> manager, DeviceNetworkPacketEvent<T> packet, ref HashSet<Device> recipients) where T : INetworkPayload
    {
        if (!manager.Comp.Devices.TryGetValue(packet.SenderAddress, out var device))
            return false;

        if (!device.DeviceData.SendBroadcastAttemptEvent)
            return true;

        var beforeBroadcastAttemptEvent = new BeforeBroadcastAttemptEvent(recipients);
        RaiseLocalEvent(packet.Sender, ref beforeBroadcastAttemptEvent, true);

        if (beforeBroadcastAttemptEvent.Cancelled || beforeBroadcastAttemptEvent.ModifiedRecipients == null)
            return false;

        recipients = beforeBroadcastAttemptEvent.ModifiedRecipients;
        return true;
    }

    private void SendToConnections<T>(ReadOnlySpan<Device> connections, DeviceNetworkPacketEvent<T> packet) where T : INetworkPayload
    {
        var xform = Transform(packet.Sender);
        var senderPos = _transformSystem.GetWorldPosition(xform);

        foreach (var connection in connections)
        {
            if (connection.Owner == packet.Sender.Owner)
                continue;

            var beforeEv = new BeforePacketSentEvent(
                packet.NetId,
                packet.Address,
                packet.Frequency,
                packet.SenderAddress,
                packet.Sender,
                xform,
                senderPos);
            RaiseLocalEvent(connection.Owner, ref beforeEv);

            if (beforeEv.Cancelled)
                continue;

            RaiseLocalEvent(connection.Owner, ref packet);
        }
    }
}
