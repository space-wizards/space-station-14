using Content.Server.DeviceNetwork.Connections;

namespace Content.Server.DeviceNetwork
{
    /// <summary>
    /// Package based device network allowing devices to communicate with eachother
    /// </summary>
    public interface IDeviceNetwork
    {
        /// <summary>
        /// Registers a device with the device network
        /// </summary>
        /// <param name="netId"><see cref="NetworkUtils"/>The id of the network to register with</param>
        /// <param name="frequency">The frequency the device receives packages on. Wired networks use frequency 0</param>
        /// <param name="messageHandler">The delegate that gets called when the device receives a message</param>
        /// <param name="receiveAll">If the device should receive all packages on its frequency or only ones addressed to itself</param>
        /// <returns></returns>
        public DeviceNetworkConnection Register(int netId, int frequency, OnReceiveNetMessage messageHandler, bool receiveAll = false);
        /// <see cref="Register(int, int, OnReceiveNetMessage, bool)"/>
        public DeviceNetworkConnection Register(int netId, OnReceiveNetMessage messageHandler, bool receiveAll = false);

        public void Update();
    }
}
