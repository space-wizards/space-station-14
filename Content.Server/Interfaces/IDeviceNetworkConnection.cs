using Content.Server.DeviceNetwork;
using System.Collections.Generic;

namespace Content.Server.Interfaces
{
    public interface IDeviceNetworkConnection
    {
        public int Frequency { get; }
        /// <summary>
        /// Sends a package to a specific device
        /// </summary>
        /// <param name="frequency">The frequency the package should be send on</param>
        /// <param name="address">The target devices address</param>
        /// <param name="payload"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public bool Send(int frequency, string address, NetworkPayload payload, Dictionary<string, object> metadata);
        /// <summary>
        /// Sends a package to a specific device
        /// <see cref="Send(int, string, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Send(int frequency, string address, NetworkPayload payload);
        /// <summary>
        /// Sends a package to a specific device
        /// <see cref="Send(int, string, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Send(string address, NetworkPayload payload);

        /// <summary>
        /// Sends a package to all devices
        /// </summary>
        /// <param name="frequency">The frequency the package should be send on</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool Broadcast(int frequency, NetworkPayload payload, Dictionary<string, object> metadata);
        /// <summary>
        /// Sends a package to all devices
        /// <see cref="Broadcast(int, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Broadcast(int frequency, NetworkPayload payload);
        /// <summary>
        /// Sends a package to all devices
        /// <see cref="Broadcast(int, NetworkPayload, Dictionary{string, object})"/>
        /// </summary>
        public bool Broadcast(NetworkPayload payload);
    }
}
