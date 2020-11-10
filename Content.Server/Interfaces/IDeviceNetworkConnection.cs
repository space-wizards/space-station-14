using Content.Server.DeviceNetwork;

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
        /// <returns></returns>
        public bool Send(int frequency, string address, NetworkPayload payload);
        /// <see cref="Send(int, string, NetworkPayload)"/>
        public bool Send(string address, NetworkPayload payload);

        /// <summary>
        /// Sends a package to all devices
        /// </summary>
        /// <param name="frequency">The frequency the package should be send on</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool Broadcast(int frequency, NetworkPayload payload);
        /// <see cref="Broadcast(int, NetworkPayload)"/>
        public bool Broadcast(NetworkPayload payload);
    }
}
