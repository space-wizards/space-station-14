using System;
using System.Collections.Generic;
using System.Text;

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
        public bool Send(int frequency, string address, Dictionary<string, string> payload);
        /// <see cref="Send(int, string, Dictionary{string, string})"/>
        public bool Send(string address, Dictionary<string, string> payload);
        /// <summary>
        /// Sends a package to all devices
        /// </summary>
        /// <param name="frequency">The frequency the package should be send on</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool Broadcast(int frequency, Dictionary<string, string> payload);
        /// <see cref="Broadcast(int, Dictionary{string, string})"/>
        public bool Broadcast(Dictionary<string, string> payload);
    }
}
