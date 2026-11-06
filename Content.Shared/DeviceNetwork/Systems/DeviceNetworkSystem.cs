using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// Entity system that handles everything device network related.
/// Device networking allows machines and devices to communicate with each other
/// while adhering to restrictions like range or being connected to the same power network.
/// </summary>
public sealed partial class DeviceNetworkSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metaSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceQuery;
    [Dependency] private EntityQuery<DeviceNetworkManagerComponent> _deviceManagerQuery;

    /// <summary>
    /// A dictionary of occupied Device addresses and corresponding <see cref="EntityUid"/>.
    /// The entity stores is not guaranteed to be valid.
    /// </summary>
    private readonly Dictionary<int, EntityUid> _occupiedAddresses = new();

    /// <summary>
    /// Cache for storing selected devices and sending a packet to them.
    /// </summary>
    private Device[] _deviceCache = [];

    /// <summary>
    /// Cache for network managers. Used by API methods.
    /// </summary>
    private readonly List<Entity<DeviceNetworkManagerComponent>> _managerCache = new();

    /// <summary>
    /// Frees all addresses on round restart.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _occupiedAddresses.Clear();
    }

    /// <summary>
    /// Checks whether the device is jammed to cancel the packet.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforePacketReceived(Entity<DeviceNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (ent.Comp.JammedTimeout != null
            && _timing.CurTime < ent.Comp.JammedTimeout.Value)
            args.Cancelled = true;
    }

    /// <summary>
    /// Shows a device address on examine.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnExamine(Entity<DeviceNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminableAddress)
            args.PushText(Loc.GetString(ent.Comp.ExamineMessage, ("address", DeviceLocalizationHelpers.GetAddressFromId(ent.Comp))));
    }

    /// <summary>
    /// Resolves the frequencies and attempts to connect some devices when a map starts.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && _protoMan.TryIndex(device.ReceiveFrequencyId, out var receive))
        {
            device.ReceiveFrequency = new DeviceFrequency(receive.Frequency);
        }

        if (device.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && _protoMan.TryIndex(device.TransmitFrequencyId, out var xmit))
        {
            device.TransmitFrequency = new DeviceFrequency(xmit.Frequency);
        }

        DirtyFields(ent.AsNullable(),
            null,
            nameof(DeviceNetworkComponent.ReceiveFrequency),
            nameof(DeviceNetworkComponent.TransmitFrequency));

        if (!ent.Comp.AutoConnect)
            return;

        ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Connects devices after they enter PVS on the client.
    /// This is needed to synchronize the state of server and client versions of
    /// <see cref="DeviceNetworkManagerComponent"/> without fully networking it.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStartup(Entity<DeviceNetworkComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsClient && ent.Comp.ConnectedNets.Count > 0)
            ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Disconnects the device from all networks on its component shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<DeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        DisconnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Adds an initialized device network manager to global PVS override.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnManagerMapInit(Entity<DeviceNetworkManagerComponent> ent, ref MapInitEvent args)
    {
        _pvsOverride.AddGlobalOverride(ent.Owner);
    }

    /// <summary>
    /// Sends a <see cref="DeviceNetworkPacketEvent{T}"/> event to all currently available and targeted devices.
    /// </summary>
    /// <param name="packet">The event to raise on all receiving devices.</param>
    /// <typeparam name="T">Type of the network payload sent with the packet.</typeparam>
    private void SendPacket<T>(ref DeviceNetworkPacketEvent<T> packet) where T : INetworkPayload
    {
        if (!TryGetNetwork(packet.Sender.AsNullable(), packet.NetId, out var network))
            return;

        if (packet.Address == null)
        {
            // Broadcast to all listening devices
            if (!network.Value.Comp.ListeningDevices.TryGetValue(packet.Frequency, out var devices)
                || !CheckRecipientsList(packet, ref devices))
                return;

            Extensions.EnsureLength(ref _deviceCache, devices.Count);
            devices.CopyTo(_deviceCache);
            SendToConnections(_deviceCache.AsSpan(0, devices.Count), packet);
        }
        else
        {
            // Send to devices that receive everything, and also the target device address
            var totalDevices = 0;
            var hasTargetedDevice = false;
            if (network.Value.Comp.ReceiveAllDevices.TryGetValue(packet.Frequency, out var devices))
            {
                totalDevices += devices.Count;
            }

            if (!TryGetDevice(network.Value, packet.Address.Value, out var device))
                return;

            if (!device.Value.ReceiveAll &&
                device.Value.ReceiveFrequency == packet.Frequency)
            {
                totalDevices += 1;
                hasTargetedDevice = true;
            }

            Extensions.EnsureLength(ref _deviceCache, totalDevices);
            devices?.CopyTo(_deviceCache);
            if (hasTargetedDevice)
                _deviceCache[totalDevices - 1] = device.Value;

            SendToConnections(_deviceCache.AsSpan(0, totalDevices), packet);
        }
    }

    /// <summary>
    /// Sends the <see cref="BeforeBroadcastAttemptEvent"/> to the sending entity if the packets SendBeforeBroadcastAttemptEvent field is set to true.
    /// The recipients are set to the modified recipient list.
    /// </summary>
    /// <returns>False if the broadcast was canceled.</returns>
    private bool CheckRecipientsList<T>(DeviceNetworkPacketEvent<T> packet, ref HashSet<Device> recipients) where T : INetworkPayload
    {
        if (!packet.Sender.Comp.SendBroadcastAttemptEvent)
            return true;

        var beforeBroadcastAttemptEvent = new BeforeBroadcastAttemptEvent(recipients);
        RaiseLocalEvent(packet.Sender, ref beforeBroadcastAttemptEvent, true);

        if (beforeBroadcastAttemptEvent.Cancelled || beforeBroadcastAttemptEvent.ModifiedRecipients == null)
            return false;

        recipients = beforeBroadcastAttemptEvent.ModifiedRecipients;
        return true;
    }

    /// <summary>
    /// Raises a <see cref="DeviceNetworkPacketEvent{T}"/> on a span of <see cref="Device"/>s.
    /// </summary>
    /// <param name="connections">A span of devices to raise the event on.</param>
    /// <param name="packet">The event to raise on all entities.</param>
    /// <typeparam name="T">Type of the network payload sent with the packet.</typeparam>
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
