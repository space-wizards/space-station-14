using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class DeviceNetworkConnection
    {
        private readonly DeviceNetwork _network;
        [ViewVariables]
        private readonly int _netId;

        [ViewVariables]
        public bool Open { get; internal set; }
        [ViewVariables]
        public string Address { get; internal set; }
        [ViewVariables]
        public int Frequency { get; internal set; }

        [ViewVariables]
        public bool RecieveAll
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

        public bool Send(int frequency, string address, IReadOnlyDictionary<string, string> payload)
        {
            return _network.EnqueuePackage(_netId, frequency, address, payload, Address);
        }

        public bool Send(string address, IReadOnlyDictionary<string, string> payload)
        {
            return Send(0, address, payload);
        }

        public bool Broadcast(int frequency, IReadOnlyDictionary<string, string> payload)
        {
            return _network.EnqueuePackage(_netId, frequency, "", payload, Address, true);
        }

        public bool Broadcast(IReadOnlyDictionary<string, string> payload)
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
