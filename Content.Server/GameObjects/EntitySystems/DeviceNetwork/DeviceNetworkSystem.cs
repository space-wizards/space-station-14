using Content.Server.GameObjects.Components.DeviceNetwork;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    [UsedImplicitly]
    public class ConnectionNetworkSystem : EntitySystem
    {

        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly Dictionary<int, List<BaseNetworkComponent>> _connections = new();
        private readonly Queue<NetworkPacket> _packets = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BaseNetworkComponent, ComponentStartup>(OnNetworkStarted);
            SubscribeLocalEvent<BaseNetworkComponent, ComponentShutdown>(OnNetworkShutdown);
            SubscribeLocalEvent<BaseNetworkComponent, ConnectEvent>(OnConnected);
            SubscribeLocalEvent<BaseNetworkComponent, DisconnectEvent>(OnDisconnected);
            SubscribeLocalEvent<BaseNetworkComponent, QueuePacketEvent>(OnQueuePacket);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var packet in _packets)
            {
                if(packet.Broadcast)
                {
                    BroadcastPacket(packet);
                    continue;
                }
                SendPacket(packet);
            }
        }

        private void OnNetworkStarted(EntityUid uid, BaseNetworkComponent component, ComponentStartup args)
        {
            AddConnection(component);
        }

        private void OnNetworkShutdown(EntityUid uid, BaseNetworkComponent component, ComponentShutdown args)
        {
            RemoveConnection(component);
        }

        private void OnConnected(EntityUid uid, BaseNetworkComponent component, ConnectEvent args)
        {
            AddConnection(component);
        }

        private void OnDisconnected(EntityUid uid, BaseNetworkComponent component, DisconnectEvent args)
        {
            RemoveConnection(component);
        }

        private void OnQueuePacket(EntityUid uid, BaseNetworkComponent component, QueuePacketEvent args)
        {
            var packet = new NetworkPacket
            {
                NetId = component.DeviceNetID,
                Address = args.Address,
                Frequency = args.Frequency,
                Broadcast = args.Broadcast,
                Data = args.Data,
                Sender = component
            };

            _packets.Enqueue(packet);
        }

        private bool AddConnection(BaseNetworkComponent connection)
        {
            var netId = connection.DeviceNetID;
            if (!_connections.ContainsKey(netId))
                _connections[netId] = new List<BaseNetworkComponent>();

            if (!_connections[netId].Contains(connection))
            {
                connection.Address = GenerateValidAddress(netId, connection.Frequency);
                _connections[netId].Add(connection);
                return true;
            }

            return false;
        }

        private bool RemoveConnection(BaseNetworkComponent connection)
        {
            connection.Address = "";
            return _connections[connection.DeviceNetID].Remove(connection);
        }

        private string GenerateValidAddress(int netId, int frequency)
        {
            var unique = false;
            var connections = ConnectionsForFrequency(netId, frequency);
            var address = "";

            while (!unique)
            {
                address = _random.Next().ToString("x");
                unique = !connections.Exists(connection => connection.Address == address);
            }

            return address;
        }

        private List<BaseNetworkComponent> ConnectionsForFrequency(int frequency, int netId)
        {
            if (!_connections.ContainsKey(netId))
                return new List<BaseNetworkComponent>();

            var result = _connections[netId].FindAll(connection => connection.Frequency == frequency);

            return result;
        }

        private bool TryGetConnectionWithAddress(int netId, int frequency, string address, [NotNullWhen(true)] out BaseNetworkComponent connection)
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

        private List<BaseNetworkComponent> ConnectionsWithReceiveAll(int netId, int frequency)
        {
            if (!_connections.ContainsKey(netId))
                return new List<BaseNetworkComponent>();

            var result = _connections[netId].FindAll(device => device.Frequency == frequency && device.ReceiveAll);

            return result;
        }

        private void SendPacket(NetworkPacket packet)
        {
            if (!TryGetConnectionWithAddress(packet.NetId, packet.Frequency, packet.Address, out var connection))
                return;

            var receivers = ConnectionsWithReceiveAll(packet.Frequency, packet.NetId);
            receivers.Add(connection);

            SendToConnections(receivers, packet);
        }

        private void BroadcastPacket(NetworkPacket packet)
        {
            var receivers = ConnectionsForFrequency(packet.Frequency, packet.NetId);
            SendToConnections(receivers, packet);
        }

        private void SendToConnections(List<BaseNetworkComponent> connections, NetworkPacket packet)
        {
            foreach (var connection in connections)
            {
                var beforeEvent = new BeforePacketSentEvent(packet.Sender.Owner);
                RaiseLocalEvent(connection.Owner.Uid, beforeEvent, false);

                if (!beforeEvent.Cancelled)
                {
                    RaiseLocalEvent(connection.Owner.Uid, new PacketSentEvent(connection.Frequency, packet.Sender.Address) , false);
                }
            }
        }

        internal struct NetworkPacket
        {
            public int NetId;
            public int Frequency;
            public string Address;
            public bool Broadcast;
            public NetworkPayload Data;
            //public Dictionary<string, object> Metadata;
            public BaseNetworkComponent Sender;
        }

    }

    public class QueuePacketEvent : EntityEventArgs
    {
        /// <summary>
        /// </summary>
        public string Address { get; init; }

        /// <summary>
        /// </summary>
        public int Frequency { get; init; }

        /// <summary>
        /// </summary>
        public bool Broadcast { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public NetworkPayload Data { get; init; }

        /// <summary>
        /// </summary>
        //public Dictionary<string, object> Metadata { get; init; };

        public QueuePacketEvent(string address, int frequency, NetworkPayload data, bool broadcast = false)
        {
            Address = address;
            Frequency = frequency;
            Data = data;
            Broadcast = broadcast;
        }
    }

    public class DisconnectEvent : EntityEventArgs
    {
    }

    public class ConnectEvent : EntityEventArgs
    {
    }

    public class BeforePacketSentEvent : CancellableEntityEventArgs
    {
        public IEntity Sender;

        public BeforePacketSentEvent(IEntity sender)
        {
            Sender = sender;
        }
    }

    public class PacketSentEvent : EntityEventArgs
    {
        //package.Frequency, package.SenderAddress, package.Data, package.Metadata, broadcast
        public int Frequency;

        public string SenderAddress;

        public PacketSentEvent(int frequency, string senderAddress)
        {
            Frequency = frequency;
            SenderAddress = senderAddress;
        }

    }
}
