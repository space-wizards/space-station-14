using Content.Server.Interfaces;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;

namespace Content.Server.DeviceNetwork
{
    public delegate void OnReceiveNetMessage(int frequency, string sender, NetworkPayload payload, Metadata metadata, bool broadcast);

    public class DeviceNetwork : IDeviceNetwork
    {
        private const int PACKAGES_PER_TICK = 30;
        private const int MAX_PACKET_COUNT = 100;
        private const int MAX_DATA_COUNT = 100;

        [Dependency] private readonly IRobustRandom _random = default!;
        private readonly Dictionary<int, List<NetworkDevice>> _devices = new Dictionary<int, List<NetworkDevice>>();
        private readonly Queue<NetworkPacket> _packets = new Queue<NetworkPacket>();

        /// <inheritdoc/>
        public DeviceNetworkConnection Register(int netId, int frequency, OnReceiveNetMessage messageHandler, bool receiveAll = false)
        {
            var address = GenerateValidAddress(netId, frequency);

            var device = new NetworkDevice
            {
                Address = address,
                Frequency = frequency,
                ReceiveAll = receiveAll,
                ReceiveNetMessage = messageHandler
            };

            AddDevice(netId, device);

            return new DeviceNetworkConnection(this, netId, address, frequency);
        }

        public DeviceNetworkConnection Register(int netId, OnReceiveNetMessage messageHandler, bool receiveAll = false)
        {
            return Register(netId, 0, messageHandler, receiveAll);
        }

        public void Update()
        {
            var count = Math.Min(PACKAGES_PER_TICK, _packets.Count);
            for (var i = 0; i < count; i++)
            {
                var package = _packets.Dequeue();

                if (package.Broadcast)
                {
                    BroadcastPackage(package);
                    continue;
                }

                SendPackage(package);
            }
        }

        public bool EnqueuePackage(int netId, int frequency, string address, NetworkPayload payload, string sender, Metadata metadata, bool broadcast = false)
        {
            if (!_devices.ContainsKey(netId) || payload.Count > MAX_DATA_COUNT || _packets.Count > MAX_PACKET_COUNT)
                return false;

            var packet = new NetworkPacket()
            {
                NetId = netId,
                Frequency = frequency,
                Address = address,
                Broadcast = broadcast,
                Payload = payload,
                Sender = sender,
                Metadata = metadata
            };
            
            _packets.Enqueue(packet);
            return true;
        }

        public void RemoveDevice(int netId, int frequency, string address)
        {
            var device = DeviceWithAddress(netId, frequency, address);
            _devices[netId].Remove(device);
        }

        public void SetDeviceReceiveAll(int netId, int frequency, string address, bool receiveAll)
        {
            var device = DeviceWithAddress(netId, frequency, address);
            device.ReceiveAll = receiveAll;
        }

        public bool GetDeviceReceiveAll(int netId, int frequency, string address)
        {
            var device = DeviceWithAddress(netId, frequency, address);
            return device.ReceiveAll;
        }

        private string GenerateValidAddress(int netId, int frequency)
        {
            var unique = false;
            var devices = DevicesForFrequency(netId, frequency);
            var address = "";

            while (!unique)
            {
                address = _random.Next().ToString("x");
                unique = !devices.Exists(device => device.Address == address);
            }

            return address;
        }

        private void AddDevice(int netId, NetworkDevice networkDevice)
        {
            if(!_devices.ContainsKey(netId))
                _devices[netId] = new List<NetworkDevice>();

            _devices[netId].Add(networkDevice);
        }

        private List<NetworkDevice> DevicesForFrequency(int netId, int frequency)
        {
            if (!_devices.ContainsKey(netId))
                return new List<NetworkDevice>();

            var result = _devices[netId].FindAll(device => device.Frequency == frequency);

            return result;
        }

        private NetworkDevice DeviceWithAddress(int netId, int frequency, string address)
        {
            var devices = DevicesForFrequency(netId, frequency);

            var device = devices.Find(device => device.Address == address);

            return device;
        }

        private List<NetworkDevice> DevicesWithReceiveAll(int netId, int frequency)
        {
            if (!_devices.ContainsKey(netId))
                return new List<NetworkDevice>();

            var result = _devices[netId].FindAll(device => device.Frequency == frequency && device.ReceiveAll);

            return result;
        }

        private void BroadcastPackage(NetworkPacket packet)
        {
            var devices = DevicesForFrequency(packet.NetId, packet.Frequency);
            SendToDevices(devices, packet, true);
        }

        private void SendPackage(NetworkPacket packet)
        {
            var devices = DevicesWithReceiveAll(packet.NetId, packet.Frequency);
            var device = DeviceWithAddress(packet.NetId, packet.Frequency, packet.Address);

            devices.Add(device);

            SendToDevices(devices, packet, false);
        }

        private void SendToDevices(List<NetworkDevice> devices, NetworkPacket packet, bool broadcast)
        {
            for (var index = 0; index < devices.Count; index++)
            {
                var device = devices[index];
                if (device.Address == packet.Sender)
                    continue;

                device.ReceiveNetMessage(packet.Frequency, packet.Sender, packet.Payload, packet.Metadata, broadcast);
            }
        }

        internal class NetworkDevice
        {
            public int Frequency;
            public string Address;
            public OnReceiveNetMessage ReceiveNetMessage;
            public bool ReceiveAll;
        }

        internal class NetworkPacket
        {
            public int NetId;
            public int Frequency;
            public string Address;
            public bool Broadcast;
            public NetworkPayload Payload { get; set; }
            public Metadata Metadata;
            public string Sender;
        }
    }
}
