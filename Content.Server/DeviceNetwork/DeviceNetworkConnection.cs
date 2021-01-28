using Content.Server.Interfaces;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.DeviceNetwork
{
    public class DeviceNetworkConnection
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

        /// <summary>
        /// Sends a package to a specific device
        /// </summary>
        /// <param name="frequency">The frequency the package should be send on</param>
        /// <param name="address">The target devices address</param>
        /// <param name="payload"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public bool Send(int frequency, string address, NetworkPayload payload, Dictionary<string, object> metadata)
        {
            return Open && _network.EnqueuePackage(_netId, frequency, address, payload, Address, metadata);
        }

        /// <summary>
        /// Sends a package to a specific device
        /// <see cref="Send(int, string, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Send(int frequency, string address, NetworkPayload payload)
        {
            return Send(frequency, address, payload);
        }

        /// <summary>
        /// Sends a package to a specific device
        /// <see cref="Send(int, string, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Send(string address, NetworkPayload payload)
        {
            return Send(0, address, payload);
        }

        /// <summary>
        /// Sends a package to all devices
        /// </summary>
        /// <param name="frequency">The frequency the package should be send on</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool Broadcast(int frequency, NetworkPayload payload, Dictionary<string, object> metadata)
        {
            return Open && _network.EnqueuePackage(_netId, frequency, "", payload, Address, metadata, true);
        }

        /// <summary>
        /// Sends a package to all devices
        /// <see cref="Broadcast(int, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Broadcast(int frequency, NetworkPayload payload)
        {
            return Broadcast(frequency, payload);
        }

        /// <summary>
        /// Sends a package to all devices
        /// <see cref="Broadcast(int, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Broadcast(NetworkPayload payload)
        {
            return Broadcast(0, payload);
        }

        /// <summary>
        /// Removes this connection from the device network and marks it as closed.
        /// </summary>
        public void Close()
        {
            _network.RemoveDevice(_netId, Frequency, Address);
            Open = false;
        }
    }
}
