using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.DeviceNetwork.Connections;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.DeviceNetwork
{
    public delegate void OnReceiveNetMessage(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast);

    public class DeviceNetwork : IDeviceNetwork
    {
        private const int PACKAGES_PER_TICK = 30;

        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly Dictionary<int, List<NetworkDevice>> _devices = new();
        private readonly Queue<NetworkPackage> _packages = new();

        /// <inheritdoc/>
        public DeviceNetworkConnection Register(int netId, int frequency, OnReceiveNetMessage messageHandler, bool receiveAll = false)
        {
            var address = GenerateValidAddress(netId, frequency);
            var device = new NetworkDevice(frequency, address, messageHandler, receiveAll);

            AddDevice(netId, device);

            return new DeviceNetworkConnection(this, netId, address, frequency);
        }

        public DeviceNetworkConnection Register(int netId, OnReceiveNetMessage messageHandler, bool receiveAll = false)
        {
            return Register(netId, 0, messageHandler, receiveAll);
        }

        public void Update()
        {
            var count = Math.Min(PACKAGES_PER_TICK, _packages.Count);
            for (var i = 0; i < count; i++)
            {
                var package = _packages.Dequeue();

                if (package.Broadcast)
                {
                    BroadcastPackage(package);
                    continue;
                }

                SendPackage(package);
            }
        }

        public bool EnqueuePackage(int netId, int frequency, string address, IReadOnlyDictionary<string, string> data, string sender, Metadata metadata, bool broadcast = false)
        {
            if (!_devices.ContainsKey(netId))
                return false;

            var package = new NetworkPackage(netId, frequency, address, broadcast, data, metadata, sender);

            _packages.Enqueue(package);
            return true;
        }

        public void RemoveDevice(int netId, int frequency, string address)
        {
            if (TryDeviceWithAddress(netId, frequency, address, out var device))
            {
                _devices[netId].Remove(device);
            }
        }

        public void SetDeviceReceiveAll(int netId, int frequency, string address, bool receiveAll)
        {
            if (TryDeviceWithAddress(netId, frequency, address, out var device))
            {
                device.ReceiveAll = receiveAll;
            }
        }

        public bool GetDeviceReceiveAll(int netId, int frequency, string address)
        {
            if (TryDeviceWithAddress(netId, frequency, address, out var device))
            {
                return device.ReceiveAll;
            }

            return false;
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

        private NetworkDevice? DeviceWithAddress(int netId, int frequency, string address)
        {
            var devices = DevicesForFrequency(netId, frequency);

            var device = devices.Find(dvc => dvc.Address == address);

            return device;
        }

        private bool TryDeviceWithAddress(int netId, int frequency, string address,
            [NotNullWhen(true)] out NetworkDevice? device)
        {
            return (device = DeviceWithAddress(netId, frequency, address)) != null;
        }

        private List<NetworkDevice> DevicesWithReceiveAll(int netId, int frequency)
        {
            if (!_devices.ContainsKey(netId))
                return new List<NetworkDevice>();

            var result = _devices[netId].FindAll(device => device.Frequency == frequency && device.ReceiveAll);

            return result;
        }

        private void BroadcastPackage(NetworkPackage package)
        {
            var devices = DevicesForFrequency(package.NetId, package.Frequency);
            SendToDevices(devices, package, true);
        }

        private void SendPackage(NetworkPackage package)
        {
            var devices = DevicesWithReceiveAll(package.NetId, package.Frequency);

            if (TryDeviceWithAddress(package.NetId, package.Frequency, package.Address, out var device))
            {
                devices.Add(device);
            }

            SendToDevices(devices, package, false);
        }

        private void SendToDevices(List<NetworkDevice> devices, NetworkPackage package, bool broadcast)
        {
            foreach (var device in devices)
            {
                if (device.Address == package.Sender)
                    continue;

                device.ReceiveNetMessage(package.Frequency, package.Sender, package.Data, package.Metadata, broadcast);
            }
        }

        internal class NetworkDevice
        {
            internal NetworkDevice(int frequency, string address, OnReceiveNetMessage receiveNetMessage, bool receiveAll)
            {
                Frequency = frequency;
                Address = address;
                ReceiveNetMessage = receiveNetMessage;
                ReceiveAll = receiveAll;
            }

            public int Frequency;
            public string Address;
            public OnReceiveNetMessage ReceiveNetMessage;
            public bool ReceiveAll;
        }

        internal class NetworkPackage
        {
            internal NetworkPackage(
                int netId,
                int frequency,
                string address,
                bool broadcast,
                IReadOnlyDictionary<string, string> data,
                Metadata metadata,
                string sender)
            {
                NetId = netId;
                Frequency = frequency;
                Address = address;
                Broadcast = broadcast;
                Data = data;
                Metadata = metadata;
                Sender = sender;
            }

            public int NetId;
            public int Frequency;
            public string Address;
            public bool Broadcast;
            public IReadOnlyDictionary<string, string> Data { get; set; }
            public Metadata Metadata;
            public string Sender;
        }
    }
}
