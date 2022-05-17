using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using static Content.Server.DeviceNetwork.Components.DeviceNetworkComponent;

namespace Content.Server.DeviceNetwork.Systems
{
    /// <summary>
    ///     Entity system that handles everything device network related.
    ///     Device networking allows machines and devices to communicate with each other while adhering to restrictions like range or being connected to the same powernet.
    /// </summary>
    [UsedImplicitly]
    public sealed class DeviceNetworkSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        private readonly Dictionary<ConnectionType, DeviceNet> _networks = new();
        private readonly Queue<DeviceNetworkPacketEvent> _packets = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DeviceNetworkComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DeviceNetworkComponent, ComponentShutdown>(OnNetworkShutdown);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_packets.Count > 0)
            {
                var packet = _packets.Dequeue();

                SendPacket(packet);
            }
        }

        /// <summary>
        /// Sends the given payload as a device network packet to the entity with the given address and frequency.
        /// Addresses are given to the DeviceNetworkComponent of an entity when connecting.
        /// </summary>
        /// <param name="uid">The EntityUid of the sending entity</param>
        /// <param name="address">The address of the entity that the packet gets sent to. If null, the message is broadcast to all devices on that frequency (except the sender)</param>
        /// <param name="frequency">The frequency to send on</param>
        /// <param name="data">The data to be sent</param>
        public void QueuePacket(EntityUid uid, string? address, NetworkPayload data, uint? frequency = null, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref device, false))
                return;

            if (device.Address == null)
                return;

            frequency ??= device.TransmitFrequency;

            if (frequency != null)
                _packets.Enqueue(new DeviceNetworkPacketEvent(device.DeviceNetId, address, frequency.Value, device.Address, uid, data));
        }

        /// <summary>
        /// Automatically attempt to connect some devices when a map starts.
        /// </summary>
        private void OnMapInit(EntityUid uid, DeviceNetworkComponent device, MapInitEvent args)
        {
            if (device.ReceiveFrequency == null
                && device.ReceiveFrequencyId != null
                && _protoMan.TryIndex<DeviceFrequencyPrototype>(device.ReceiveFrequencyId, out var receive))
            {
                device.ReceiveFrequency = receive.Frequency;
            }

            if (device.TransmitFrequency == null
                && device.TransmitFrequencyId != null
                && _protoMan.TryIndex<DeviceFrequencyPrototype>(device.TransmitFrequencyId, out var xmit))
            {
                device.TransmitFrequency = xmit.Frequency;
            }

            if (device.AutoConnect)
                ConnectDevice(uid, device);
        }

        /// <summary>
        /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
        /// </summary>
        private void OnNetworkShutdown(EntityUid uid, DeviceNetworkComponent component, ComponentShutdown args)
        {
            DisconnectDevice(uid, component, false);
        }

        /// <summary>
        /// Connect an entity with a DeviceNetworkComponent. Note that this will re-use an existing address if the
        /// device already had one configured. If there is a clash, the device cannot join the network.
        /// </summary>
        public bool ConnectDevice(EntityUid uid, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref device, false))
                return false;

            if (!_networks.TryGetValue(device.DeviceNetId, out var network))
            {
                network = new(device.DeviceNetId, _random);
                _networks[device.DeviceNetId] = network;
            }

            return network.Add(device);
        }

        /// <summary>
        /// Disconnect an entity with a DeviceNetworkComponent.
        /// </summary>
        public bool DisconnectDevice(EntityUid uid, DeviceNetworkComponent? device, bool preventAutoConnect = true)
        {
            if (!Resolve(uid, ref device, false))
                return false;

            // If manually disconnected, don't auto reconnect when a game state is loaded.
            if (preventAutoConnect)
                device.AutoConnect = false;

            if (!_networks.TryGetValue(device.DeviceNetId, out var network))
            {
                return false;
            }

            if (!network.Remove(device))
                return false;

            if (network.Devices.Count == 0)
                _networks.Remove(device.DeviceNetId);

            return true;
        }

        #region Get Device
        /// <summary>
        /// Get a list of devices listening on a given frequency on some network.
        /// </summary>
        private HashSet<DeviceNetworkComponent> GetListeningDevices(ConnectionType netId, uint frequency)
        {
            if (_networks.TryGetValue(netId, out var network) && network.ListeningDevices.TryGetValue(frequency, out var devices))
                return devices;

            return new();
        }

        /// <summary>
        /// Get a list of devices listening for ANY transmission on a given frequency, rather than just broadcast & addressed events.
        /// </summary>
        private HashSet<DeviceNetworkComponent> GetRecieveAllDevices(ConnectionType netId, uint frequency)
        {
            if (_networks.TryGetValue(netId, out var network) && network.ReceiveAllDevices.TryGetValue(frequency, out var devices))
                return devices;

            return new();
        }

        /// <summary>
        ///     Try to find a device on a network using its address.
        /// </summary>
        private bool TryGetDevice(ConnectionType netId, string address, [NotNullWhen(true)] out DeviceNetworkComponent? device)
        {
            if (!_networks.TryGetValue(netId, out var network))
            {
                device = null;
                return false;
            }

            return network.Devices.TryGetValue(address, out device);
        }
        #endregion

        #region Packet Sending
        private void SendPacket(DeviceNetworkPacketEvent packet)
        {
            HashSet<DeviceNetworkComponent> recipients;

            if (packet.Address == null)
            {
                // Broadcast to all listening devices
                recipients = GetListeningDevices(packet.NetId, packet.Frequency);
            }
            else
            {
                // Add devices listening to all messages
                recipients = new(GetRecieveAllDevices(packet.NetId, packet.Frequency));

                // add the intended recipient (if they are even listening).
                if (TryGetDevice(packet.NetId, packet.Address, out var device) && device.ReceiveFrequency == packet.Frequency)
                    recipients.Add(device);
            }

            SendToConnections(recipients, packet);
        }

        private void SendToConnections(HashSet<DeviceNetworkComponent> connections, DeviceNetworkPacketEvent packet)
        {
            var xform = Transform(packet.Sender);

            BeforePacketSentEvent beforeEv = new(packet.Sender, xform, _transformSystem.GetWorldPosition(xform));

            foreach (var connection in connections)
            {
                if (connection.Owner == packet.Sender)
                    continue;

                RaiseLocalEvent(connection.Owner, beforeEv, false);

                if (!beforeEv.Cancelled)
                    RaiseLocalEvent(connection.Owner, packet, false);
                else
                    beforeEv.Uncancel();
            }
        }
        #endregion

        #region Component Setter Functions
        public void SetReceiveFrequency(EntityUid uid, uint? frequency, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref device, false))
                return;

            if (device.ReceiveFrequency == frequency)
                return;

            if (!_networks.TryGetValue(device.DeviceNetId, out var deviceNet) || !deviceNet.UpdateReceiveFrequency(device.Address, frequency))
                device.ReceiveFrequency = frequency;
        }

        public void SetTransmitFrequency(EntityUid uid, uint? frequency, DeviceNetworkComponent? device = null)
        {
            if (Resolve(uid, ref device, false))
                device.TransmitFrequency = frequency;
        }

        public void SetReceiveAll(EntityUid uid, bool receiveAll, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref device, false))
                return;

            if (device.ReceiveAll == receiveAll)
                return;

            if (!_networks.TryGetValue(device.DeviceNetId, out var deviceNet) || !deviceNet.UpdateReceiveAll(device.Address, receiveAll))
                device.ReceiveAll = receiveAll;
        }

        public void SetAddress(EntityUid uid, string address, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref device, false))
                return;

            if (device.Address == address)
            {
                device.CustomAddress = true;
                return;
            }

            if (!_networks.TryGetValue(device.DeviceNetId, out var deviceNet) || !deviceNet.UpdateAddress(device.Address, address))
            {
                device.Address = address;
                device.CustomAddress = true;
            }
        }

        public void RandomizeAddress(EntityUid uid, string address, DeviceNetworkComponent? device = null)
        {
            if (!Resolve(uid, ref device, false))
                return;

            if (!_networks.TryGetValue(device.DeviceNetId, out var deviceNet) || !deviceNet.RandomizeAddress(device.Address, address))
            {
                var prefix = string.IsNullOrWhiteSpace(device.Prefix) ? null : Loc.GetString(device.Prefix);
                device.Address = $"{prefix}{_random.Next():x}";
                device.CustomAddress = false;
            }
        }
        #endregion
    }

    /// <summary>
    /// Event raised before a device network packet is send.
    /// Subscribed to by other systems to prevent the packet from being sent.
    /// </summary>
    public sealed class BeforePacketSentEvent : CancellableEntityEventArgs
    {
        /// <summary>
        /// The EntityUid of the entity the packet was sent from.
        /// </summary>
        public readonly EntityUid Sender;

        public readonly TransformComponent SenderTransform;

        /// <summary>
        ///     The senders current position in world coordinates.
        /// </summary>
        public readonly Vector2 SenderPosition;

        public BeforePacketSentEvent(EntityUid sender, TransformComponent xform, Vector2 senderPosition)
        {
            Sender = sender;
            SenderTransform = xform;
            SenderPosition = senderPosition;
        }
    }

    /// <summary>
    /// Event raised when a device network packet gets sent.
    /// </summary>
    public sealed class DeviceNetworkPacketEvent : EntityEventArgs
    {
        /// <summary>
        /// The type of network that this packet is being sent on.
        /// </summary>
        public ConnectionType NetId;

        /// <summary>
        /// The frequency the packet is sent on.
        /// </summary>
        public readonly uint Frequency;

        /// <summary>
        /// Address of the intended recipient. Null if the message was broadcast.
        /// </summary>
        public string? Address;

        /// <summary>
        /// The device network address of the sending entity.
        /// </summary>
        public readonly string SenderAddress;

        /// <summary>
        /// The entity that sent the packet.
        /// </summary>
        public EntityUid Sender;

        /// <summary>
        /// The data that is being sent.
        /// </summary>
        public readonly NetworkPayload Data;

        public DeviceNetworkPacketEvent(ConnectionType netId, string? address, uint frequency, string senderAddress, EntityUid sender, NetworkPayload data)
        {
            NetId = netId;
            Address = address;
            Frequency = frequency;
            SenderAddress = senderAddress;
            Sender = sender;
            Data = data;
        }
    }
}
