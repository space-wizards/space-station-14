using System;
using Content.Server.GameObjects.EntitySystems.DeviceNetwork;

namespace Content.Server.Interfaces
{
    public interface IDeviceNetwork
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="netId"><see cref="BaseNetworks"/></param>
        /// <param name="frequency"></param>
        /// <param name="messageHandler"></param>
        /// <param name="receiveAll"></param>
        /// <returns></returns>
        public DeviceNetworkConnection Register(int netId, int frequency, OnReceiveNetMessage messageHandler, bool receiveAll = false);
        public DeviceNetworkConnection Register(int netId, OnReceiveNetMessage messageHandler, bool receiveAll = false);

        public void Update();
    }
}
