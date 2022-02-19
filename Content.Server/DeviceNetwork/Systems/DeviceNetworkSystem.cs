using Content.Server.DeviceNetwork.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        private readonly Dictionary<DeviceNetworkComponent.ConnectionType, List<DeviceNetworkComponent>> _connections = new();
        private readonly Queue<NetworkPacket> _packets = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DeviceNetworkComponent, ComponentStartup>(OnNetworkStarted);
            SubscribeLocalEvent<DeviceNetworkComponent, ComponentShutdown>(OnNetworkShutdown);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_packets.Count > 0)
            {
                var packet = _packets.Dequeue();

                if(packet.Broadcast)
                {
                    BroadcastPacket(packet);
                    continue;
                }
                SendPacket(packet);
            }
        }

        /// <summary>
        /// Manually connect an entity with a DeviceNetworkComponent.
        /// </summary>
        /// <param name="uid">The Entity containing a DeviceNetworkComponent</param>
        public void Connect(EntityUid uid)
        {
            if (EntityManager.TryGetComponent<DeviceNetworkComponent>(uid, out var component))
            {
                AddConnection(component);
            }
        }

        /// <summary>
        /// Sends the given payload as a device network packet to the entity with the given address and frequency.
        /// Addresses are given to the DeviceNetworkComponent of an entity when connecting.
        /// </summary>
        /// <param name="uid">The EntityUid of the sending entity</param>
        /// <param name="address">The address of the entity that the packet gets sent to when not broadcasting</param>
        /// <param name="frequency">The frequency to send on</param>
        /// <param name="data">The data to be sent</param>
        /// <param name="broadcast">Send to all devices on the same device network on the given frequency</param>
        public void QueuePacket(EntityUid uid, string address, int frequency, NetworkPayload data, bool broadcast = false)
        {
            if (EntityManager.TryGetComponent<DeviceNetworkComponent>(uid, out var component))
            {
                var packet = new NetworkPacket
                {
                    NetId = component.DeviceNetId,
                    Address = address,
                    Frequency = frequency,
                    Broadcast = broadcast,
                    Data = data,
                    Sender = component
                };

                _packets.Enqueue(packet);
            }
        }

        /// <summary>
        /// Manually disconnect an entity with a DeviceNetworkComponent.
        /// </summary>
        /// <param name="uid">The Entity containing a DeviceNetworkComponent</param>
        public void Disconnect(EntityUid uid)
        {
            if (EntityManager.TryGetComponent<DeviceNetworkComponent>(uid, out var component))
            {
                RemoveConnection(component);
            }
        }

        /// <summary>
        /// Automatically connect when an entity with a DeviceNetworkComponent starts up.
        /// </summary>
        private void OnNetworkStarted(EntityUid uid, DeviceNetworkComponent component, ComponentStartup args)
        {
            AddConnection(component);
        }

        /// <summary>
        /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
        /// </summary>
        private void OnNetworkShutdown(EntityUid uid, DeviceNetworkComponent component, ComponentShutdown args)
        {
            RemoveConnection(component);
        }

        private bool AddConnection(DeviceNetworkComponent connection)
        {
            var netId = connection.DeviceNetId;
            if (!_connections.ContainsKey(netId))
                _connections[netId] = new List<DeviceNetworkComponent>();

            if (!_connections[netId].Contains(connection))
            {
                connection.Address = GenerateValidAddress(netId);
                _connections[netId].Add(connection);
                connection.Open = true;
                return true;
            }

            return false;
        }

        private bool RemoveConnection(DeviceNetworkComponent connection)
        {
            connection.Address = "";
            connection.Open = false;
            return _connections[connection.DeviceNetId].Remove(connection);
        }

        /// <summary>
        /// Generates a valid address by randomly generating one and checking if it already exists on the device network with the given device netId.
        /// </summary>
        private string GenerateValidAddress(DeviceNetworkComponent.ConnectionType netId)
        {
            var unique = false;
            var connections = _connections[netId];
            var address = "";

            while (!unique)
            {
                address = _random.Next().ToString("x");
                unique = !connections.Exists(connection => connection.Address == address);
            }

            return address;
        }

        private List<DeviceNetworkComponent> ConnectionsForFrequency(DeviceNetworkComponent.ConnectionType netId, int frequency)
        {
            if (!_connections.ContainsKey(netId))
                return new List<DeviceNetworkComponent>();

            var result = _connections[netId].FindAll(connection => connection.Frequency == frequency);

            return result;
        }

        private bool TryGetConnectionWithAddress(DeviceNetworkComponent.ConnectionType netId, int frequency, string address, [NotNullWhen(true)] out DeviceNetworkComponent connection)
        {
            var connections = ConnectionsForFrequency(netId, frequency);

            var result = connections.Find(dvc => dvc.Address == address);

            if(result != null)
            {
                connection = result;
                return true;
            }

            connection = default!;
            return false;
        }

        private List<DeviceNetworkComponent> ConnectionsWithReceiveAll(DeviceNetworkComponent.ConnectionType netId, int frequency)
        {
            if (!_connections.ContainsKey(netId))
                return new List<DeviceNetworkComponent>();

            var result = _connections[netId].FindAll(device => device.Frequency == frequency && device.ReceiveAll);

            return result;
        }

        private void SendPacket(NetworkPacket packet)
        {
            if (!TryGetConnectionWithAddress(packet.NetId, packet.Frequency, packet.Address, out var connection))
                return;

            var receivers = ConnectionsWithReceiveAll(packet.NetId, packet.Frequency);
            receivers.Add(connection);

            SendToConnections(receivers, packet);
        }

        private void BroadcastPacket(NetworkPacket packet)
        {
            var receivers = ConnectionsForFrequency(packet.NetId, packet.Frequency);
            SendToConnections(receivers, packet);
        }

        private void SendToConnections(List<DeviceNetworkComponent> connections, NetworkPacket packet)
        {
            foreach (var connection in connections)
            {
                var beforeEvent = new BeforePacketSentEvent(packet.Sender.Owner);
                RaiseLocalEvent(connection.Owner, beforeEvent, false);

                if (!beforeEvent.Cancelled)
                {
                    RaiseLocalEvent(connection.Owner, new PacketSentEvent(connection.Frequency, packet.Sender.Address, packet.Data, packet.Broadcast) , false);
                }
            }
        }

        internal struct NetworkPacket
        {
            public DeviceNetworkComponent.ConnectionType NetId;
            public int Frequency;
            public string Address;
            public bool Broadcast;
            public NetworkPayload Data;
            public DeviceNetworkComponent Sender;
        }
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
        public EntityUid Sender;

        public BeforePacketSentEvent(EntityUid sender)
        {
            Sender = sender;
        }
    }

    /// <summary>
    /// Event raised when a device network packet gets sent.
    /// </summary>
    public sealed class PacketSentEvent : EntityEventArgs
    {
        /// <summary>
        /// The frequency the packet is sent on.
        /// </summary>
        public int Frequency;

        /// <summary>
        /// The device network address of the sending entity.
        /// </summary>
        public string SenderAddress;

        /// <summary>
        /// The data that is beeing sent.
        /// </summary>
        public NetworkPayload Data;

        /// <summary>
        /// Whether the packet was broadcasted.
        /// </summary>
        public bool Broadcast;

        public PacketSentEvent(int frequency, string senderAddress, NetworkPayload data, bool broadcast)
        {
            Frequency = frequency;
            SenderAddress = senderAddress;
            Data = data;
            Broadcast = broadcast;
        }

    }
}
