using Content.Server.Interfaces;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class DeviceNetworkConnection : IDeviceNetworkConnection
    {
        private readonly DeviceNetwork _network;
        [ViewVariables]
        private readonly int _netId;

        [ViewVariables]
        public bool Open { get; private set; }
        [ViewVariables]
        public string Address { get; private set; }
        [ViewVariables]
        public int Frequency { get; private set; }

        [ViewVariables]
        public bool ReceiveAll
        {
            get => _network.GetDeviceReceiveAll(_netId, Frequency, Address);
            set => _network.SetDeviceReceiveAll(_netId, Frequency, Address, value);
        }

        public DeviceNetworkConnection(DeviceNetwork network, int netId, string address, int frequency)
        {
            _network = network;
            _netId = netId;
            Open = true;
            Address = address;
            Frequency = frequency;
        }

        public bool Send(int frequency, string address, IReadOnlyDictionary<string, string> payload, Metadata metadata)
        {
            return Open && _network.EnqueuePackage(_netId, frequency, address, payload, Address, metadata);
        }

        public bool Send(int frequency, string address, Dictionary<string, string> payload)
        {
            return Send(frequency, address, payload);
        }

        public bool Send(string address, Dictionary<string, string> payload)
        {
            return Send(0, address, payload);
        }

        public bool Broadcast(int frequency, IReadOnlyDictionary<string, string> payload, Metadata metadata)
        {
            return Open && _network.EnqueuePackage(_netId, frequency, "", payload, Address, metadata, true);
        }

        public bool Broadcast(int frequency, Dictionary<string, string> payload)
        {
            return Broadcast(frequency, payload);
        }

        public bool Broadcast(Dictionary<string, string> payload)
        {
            return Broadcast(0, payload);
        }

        public void Close()
        {
            _network.RemoveDevice(_netId, Frequency, Address);
            Open = false;
        }
    }
}
